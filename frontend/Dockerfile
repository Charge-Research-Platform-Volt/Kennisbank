# Base image with Node.js LTS on Alpine Linux
FROM node:lts-alpine AS base
ENV NEXT_TELEMETRY_DISABLED=true
ENV PORT=3000

# -- Install dependencies
FROM base AS deps
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm install
RUN npm ci

# -- Development
FROM deps AS development
ENV NODE_ENV=development
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
EXPOSE $PORT
# Create custom entrypoint script to run npm install as well to update everything
ENTRYPOINT npm install && npm run dev

# -- CI Runner
FROM deps AS ci-runner
ENV NODE_ENV=production
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
EXPOSE $PORT

# -- Build the app
FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ENV API_URL=http://backend:8080
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

# This program has been developed by students from the bachelor Computer Science at Utrecht
# University within the Software Project course.
# © Copyright Utrecht University (Department of Information and Computing Sciences)
