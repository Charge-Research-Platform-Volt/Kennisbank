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

# -- Build the app,
# Do not use the default env variables because before docker compose would normally override the, they are already being used.
# Instead pass any needed env variables as build args for the build, which will be available as env variables during the build.
FROM base-image AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build


# -- Run the app
FROM base AS production
ENV NODE_ENV=production
WORKDIR /app

# Create a non-root user
RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

# Copy only necessary files
COPY --from=builder /app/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs
EXPOSE $PORT
ENV HOSTNAME="0.0.0.0"

CMD ["node", "server.js"]