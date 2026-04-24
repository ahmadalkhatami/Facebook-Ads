-- Initial Schema for Full Auto Facebook Ads System (SCALPING ENGINE)
-- Updated: Phase 1 Complete

-- Products Table
CREATE TABLE IF NOT EXISTS products (
    id SERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    price DECIMAL(15, 2),
    source_url TEXT,
    rating DECIMAL(3, 2),
    sold_count INTEGER,
    score INTEGER DEFAULT 0,
    reason TEXT,
    status TEXT DEFAULT 'SCRAPED',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Creatives Table
CREATE TABLE IF NOT EXISTS creatives (
    id SERIAL PRIMARY KEY,
    product_id INTEGER REFERENCES products(id),
    hook TEXT,
    caption TEXT,
    video_url TEXT,
    ctr DECIMAL(10, 2) DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Campaigns Table (enhanced with product_price and budget)
CREATE TABLE IF NOT EXISTS campaigns (
    id SERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    fb_campaign_id TEXT UNIQUE,
    status TEXT DEFAULT 'ACTIVE',
    budget DECIMAL(15, 2) DEFAULT 0,
    spend DECIMAL(15, 2) DEFAULT 0,
    revenue DECIMAL(15, 2) DEFAULT 0,
    roas DECIMAL(10, 2) DEFAULT 0,
    ctr DECIMAL(10, 2) DEFAULT 0,
    product_price DECIMAL(15, 2) DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Metrics Table (historical snapshots)
CREATE TABLE IF NOT EXISTS metrics (
    id SERIAL PRIMARY KEY,
    campaign_id INTEGER REFERENCES campaigns(id),
    spend DECIMAL(15, 2) DEFAULT 0,
    revenue DECIMAL(15, 2) DEFAULT 0,
    roas DECIMAL(10, 2) DEFAULT 0,
    ctr DECIMAL(10, 2) DEFAULT 0,
    recorded_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Decision Logs Table (audit trail)
CREATE TABLE IF NOT EXISTS decision_logs (
    id SERIAL PRIMARY KEY,
    campaign_id INTEGER REFERENCES campaigns(id),
    action TEXT, -- KILL, SCALE, REPLACE_CREATIVE
    reason TEXT,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_products_status ON products(status);
CREATE INDEX IF NOT EXISTS idx_products_score ON products(score DESC);
CREATE INDEX IF NOT EXISTS idx_campaigns_status ON campaigns(status);
CREATE INDEX IF NOT EXISTS idx_campaigns_fb_id ON campaigns(fb_campaign_id);
CREATE INDEX IF NOT EXISTS idx_metrics_campaign ON metrics(campaign_id);
CREATE INDEX IF NOT EXISTS idx_metrics_recorded ON metrics(recorded_at DESC);
CREATE INDEX IF NOT EXISTS idx_decision_logs_campaign ON decision_logs(campaign_id);
CREATE INDEX IF NOT EXISTS idx_decision_logs_created ON decision_logs(created_at DESC);
