import requests
import psycopg2
import os
from dotenv import load_dotenv
import time
import logging
import json
from urllib.parse import quote

load_dotenv()

# Setup logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('/app/logs/scraper.log') if os.path.exists('/app/logs') else logging.StreamHandler(),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)

# Database Connection
def get_db_connection():
    return psycopg2.connect(
        host=os.getenv("DB_HOST", "localhost"),
        database=os.getenv("DB_NAME", "fb_ads_system"),
        user=os.getenv("DB_USER", "user"),
        password=os.getenv("DB_PASSWORD", "password")
    )

# ==========================================
# TOKOPEDIA SCRAPER (via public API endpoints)
# ==========================================
def scrape_tokopedia(keyword="trending", page=1, rows=20):
    """
    Scrape Tokopedia product data via their GraphQL-like public API.
    This uses the same endpoints that the Tokopedia website uses.
    """
    logger.info(f"Scraping Tokopedia for keyword: '{keyword}', page: {page}")
    
    url = "https://gql.tokopedia.com/graphql/SearchProductQueryV4"
    
    headers = {
        "User-Agent": "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Content-Type": "application/json",
        "X-Source": "tokopedia-lite",
        "X-Tkpd-Lite-Service": "zeus",
    }
    
    payload = [{
        "operationName": "SearchProductQueryV4",
        "variables": json.dumps({
            "params": f"device=desktop&navsource=&ob=23&page={page}&q={keyword}&related=true&rows={rows}&safe_search=false&scheme=https&shipping=&source=search&st=product&start={(page-1)*rows}&topads=0&unique_id=&user_id="
        }),
        "query": """query SearchProductQueryV4($params: String!) {
            ace_search_product_v4(params: $params) {
                header {
                    totalData
                }
                data {
                    products {
                        id
                        name
                        price {
                            text
                            number
                        }
                        shop {
                            name
                            city
                        }
                        stats {
                            reviewCount
                            rating
                            countSold
                        }
                        url
                        imageUrl
                    }
                }
            }
        }"""
    }]
    
    try:
        response = requests.post(url, json=payload, headers=headers, timeout=15)
        response.raise_for_status()
        
        data = response.json()
        products_raw = data[0].get("data", {}).get("ace_search_product_v4", {}).get("data", {}).get("products", [])
        
        products = []
        for p in products_raw:
            stats = p.get("stats", {})
            price_num = p.get("price", {}).get("number", 0)
            rating = float(stats.get("rating", 0))
            sold_text = stats.get("countSold", "0")
            
            # Parse sold count (e.g., "1rb+" -> 1000, "500+" -> 500)
            sold_count = parse_sold_count(sold_text)
            
            products.append({
                "name": p.get("name", ""),
                "price": price_num,
                "rating": rating,
                "sold": sold_count,
                "url": p.get("url", ""),
                "image_url": p.get("imageUrl", ""),
                "shop_name": p.get("shop", {}).get("name", ""),
                "shop_city": p.get("shop", {}).get("city", ""),
            })
        
        logger.info(f"Found {len(products)} products from Tokopedia")
        return products
        
    except requests.RequestException as e:
        logger.error(f"Error scraping Tokopedia: {e}")
        return []
    except (KeyError, IndexError, json.JSONDecodeError) as e:
        logger.error(f"Error parsing Tokopedia response: {e}")
        return []

# ==========================================
# SHOPEE SCRAPER (via public search API)
# ==========================================
def scrape_shopee(keyword="trending", page=0, limit=20):
    """
    Scrape Shopee product data via their public search API.
    """
    logger.info(f"Scraping Shopee for keyword: '{keyword}', page: {page}")
    
    url = "https://shopee.co.id/api/v4/search/search_items"
    
    params = {
        "by": "relevancy",
        "keyword": keyword,
        "limit": limit,
        "newest": page * limit,
        "order": "desc",
        "page_type": "search",
        "scenario": "PAGE_GLOBAL_SEARCH",
        "version": 2,
    }
    
    headers = {
        "User-Agent": "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Accept": "application/json",
        "X-Requested-With": "XMLHttpRequest",
        "Referer": f"https://shopee.co.id/search?keyword={quote(keyword)}",
    }
    
    try:
        response = requests.get(url, params=params, headers=headers, timeout=15)
        response.raise_for_status()
        
        data = response.json()
        items = data.get("items", [])
        
        products = []
        for item in items:
            info = item.get("item_basic", {})
            
            price = info.get("price", 0) / 100000  # Shopee stores price in units of 100000
            rating = round(info.get("item_rating", {}).get("rating_star", 0), 2)
            sold = info.get("historical_sold", 0) or info.get("sold", 0)
            
            products.append({
                "name": info.get("name", ""),
                "price": price,
                "rating": rating,
                "sold": sold,
                "url": f"https://shopee.co.id/-i.{info.get('shopid', '')}.{info.get('itemid', '')}",
                "image_url": f"https://cf.shopee.co.id/file/{info.get('image', '')}",
                "shop_name": "",
                "shop_city": info.get("shop_location", ""),
            })
        
        logger.info(f"Found {len(products)} products from Shopee")
        return products
        
    except requests.RequestException as e:
        logger.error(f"Error scraping Shopee: {e}")
        return []
    except (KeyError, json.JSONDecodeError) as e:
        logger.error(f"Error parsing Shopee response: {e}")
        return []

def parse_sold_count(sold_text):
    """Parse Indonesian sold count text: '1rb+' -> 1000, '500+' -> 500, '2,5rb' -> 2500"""
    if not sold_text:
        return 0
    
    sold_text = str(sold_text).lower().replace("+", "").replace(".", "").replace(",", ".").strip()
    
    try:
        if "rb" in sold_text:
            return int(float(sold_text.replace("rb", "")) * 1000)
        elif "jt" in sold_text:
            return int(float(sold_text.replace("jt", "")) * 1000000)
        else:
            return int(float(sold_text))
    except (ValueError, TypeError):
        return 0

def filter_products(products, min_rating=4.5, min_sold=1000):
    """Filter products based on quality criteria"""
    filtered = [
        p for p in products
        if p["rating"] >= min_rating and p["sold"] >= min_sold
    ]
    logger.info(f"Filtered {len(products)} -> {len(filtered)} products (rating>={min_rating}, sold>={min_sold})")
    return filtered

def save_products(products):
    """Save filtered products to database, avoiding duplicates"""
    if not products:
        logger.info("No products to save.")
        return 0
    
    conn = get_db_connection()
    cur = conn.cursor()
    saved = 0
    
    for p in products:
        try:
            # Check for duplicates by name (simple dedup)
            cur.execute("SELECT id FROM products WHERE name = %s AND status != 'LAUNCHED'", (p["name"],))
            if cur.fetchone():
                logger.debug(f"Skipping duplicate: {p['name']}")
                continue
            
            cur.execute(
                """INSERT INTO products (name, price, source_url, rating, sold_count, status) 
                   VALUES (%s, %s, %s, %s, %s, 'SCRAPED')""",
                (p["name"], p["price"], p["url"], p["rating"], p["sold"])
            )
            saved += 1
        except Exception as e:
            logger.error(f"Error saving product {p.get('name', 'unknown')}: {e}")
            conn.rollback()
            continue
    
    conn.commit()
    cur.close()
    conn.close()
    logger.info(f"✅ Saved {saved} new products to database.")
    return saved

# ==========================================
# TRENDING KEYWORDS for discovery
# ==========================================
TRENDING_KEYWORDS = [
    "gadget unik",
    "alat dapur viral",
    "aksesoris hp",
    "peralatan rumah tangga",
    "skincare viral",
    "fashion wanita",
    "tas selempang",
    "lampu tidur unik",
    "organizer",
    "tools multifungsi",
]

def main():
    logger.info("=" * 50)
    logger.info("🚀 Product Scraper Started")
    logger.info("=" * 50)
    
    cycle = 0
    while True:
        cycle += 1
        logger.info(f"\n--- Scrape Cycle #{cycle} ---")
        
        all_products = []
        
        # Rotate through keywords
        keyword = TRENDING_KEYWORDS[(cycle - 1) % len(TRENDING_KEYWORDS)]
        logger.info(f"Using keyword: '{keyword}'")
        
        # Try Tokopedia first
        tokopedia_products = scrape_tokopedia(keyword=keyword, rows=30)
        all_products.extend(tokopedia_products)
        
        # Small delay to be respectful
        time.sleep(2)
        
        # Then try Shopee
        shopee_products = scrape_shopee(keyword=keyword, limit=30)
        all_products.extend(shopee_products)
        
        logger.info(f"Total scraped: {len(all_products)} products")
        
        # Filter by quality criteria
        filtered = filter_products(all_products)
        
        # Save to database
        if filtered:
            saved = save_products(filtered)
            logger.info(f"Cycle #{cycle} complete. Saved {saved} new products.")
        else:
            logger.info(f"Cycle #{cycle} complete. No products met criteria.")
        
        # Scrape every 6 hours as per spec
        logger.info(f"💤 Sleeping for 6 hours until next scrape cycle...")
        time.sleep(60 * 60 * 6)

if __name__ == "__main__":
    main()
