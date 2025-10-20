# Base image with Node.js LTS on Alpine Linux
FROM node:lts-alpine AS base-image
ENV NEXT_TELEMETRY_DISABLED=1 

# Define the base with env variable defaults
FROM base-image AS base
ENV API_URL=http://localhost:3000
ENV PORT=3001

# -- Install dependencies
FROM base AS deps
WORKDIR /app
COPY package.json package-lock.json* source.config.ts ./
RUN npm install
RUN npm ci

# -- Development
FROM deps AS development
ENV NODE_ENV=development
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
EXPOSE $PORT
# Create custom entrypoint script to run npm install as well to update everything. WARNING: Might silently fail on npm install
ENTRYPOINT npm install && npm run dev --turbopack

# -- CI Runner
FROM deps AS ci-runner
ENV NODE_ENV=production
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
EXPOSE $PORT

# -- Build the app for static export
FROM base-image AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

# -- Serve static files with nginx
FROM nginx:alpine AS production
ENV NODE_ENV=production

# Copy the static export (out directory) to nginx html directory
COPY --from=builder /app/out /usr/share/nginx/html

# Create custom nginx configuration for SPA routing
RUN echo 'server { \
    listen 3001; \
    server_name localhost; \
    root /usr/share/nginx/html; \
    index index.html; \
    \
    # Handle client-side routing \
    location / { \
        try_files $uri $uri/ $uri.html /index.html; \
    } \
    \
    # Security headers \
    add_header X-Frame-Options "SAMEORIGIN" always; \
    add_header X-XSS-Protection "1; mode=block" always; \
    add_header X-Content-Type-Options "nosniff" always; \
    add_header Referrer-Policy "no-referrer-when-downgrade" always; \
    \
    # Cache static assets \
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg)$ { \
        expires 1y; \
        add_header Cache-Control "public, immutable"; \
    } \
    \
    # Gzip compression \
    gzip on; \
    gzip_vary on; \
    gzip_min_length 1024; \
    gzip_proxied expired no-cache no-store private must-revalidate auth; \
    gzip_types text/plain text/css text/xml text/javascript application/javascript application/xml+rss application/json; \
}' > /etc/nginx/conf.d/default.conf

EXPOSE 3001

CMD ["nginx", "-g", "daemon off;"]