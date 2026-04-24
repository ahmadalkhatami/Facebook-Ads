const TelegramBot = require('node-telegram-bot-api');
const Redis = require('ioredis');
require('dotenv').config();

const token = process.env.TELEGRAM_TOKEN || 'YOUR_TOKEN';
const chatId = process.env.TELEGRAM_CHAT_ID || 'YOUR_CHAT_ID';

const bot = new TelegramBot(token, { polling: true });
const redis = new Redis(process.env.REDIS_URL || 'redis://localhost:6379');

console.log('Telegram Bot is starting...');

// Listen for messages from Redis (pub/sub)
redis.subscribe('fb_ads_alerts', (err, count) => {
    if (err) {
        console.error('Failed to subscribe: %s', err.message);
    } else {
        console.log(`Subscribed successfully! This client is currently subscribed to ${count} channels.`);
    }
});

redis.on('message', (channel, message) => {
    console.log(`Received message from ${channel}: ${message}`);
    const alert = JSON.parse(message);
    
    const text = `🔥 *Facebook Ads Alert* 🔥\n\n` +
                 `*Action:* ${alert.action}\n` +
                 `*Campaign:* ${alert.campaign_name}\n` +
                 `*Reason:* ${alert.reason}\n` +
                 `*Details:* ${JSON.stringify(alert.details)}`;

    bot.sendMessage(chatId, text, { parse_mode: 'Markdown' });
});

bot.onText(/\/start/, (msg) => {
    bot.sendMessage(msg.chat.id, 'Welcome to FB Ads Auto Bot! I will notify you of any campaign actions.');
});

console.log('Bot is listening for Redis alerts...');
