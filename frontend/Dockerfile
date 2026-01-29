# Base image with Node.js LTS on Alpine Linux
FROM node:lts-alpine AS base-image
ENV NEXT_TELEMETRY_DISABLED=1

# Define the base with env variable defaults
FROM base-image AS base
ENV API_URL=http://backend:8080
ENV HELP_URL=http://localhost:3001
ENV NEXT_PUBLIC_TIMEZONE=UTC
ENV PORT=3000

# -- Install dependencies
FROM base AS deps
WORKDIR /app
# Install build dependencies for sharp on Alpine
RUN apk add --no-cache libc6-compat
COPY package.json package-lock.json* ./
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

# -- Build the app,
# Do not use the default env variables because before docker compose would normally override the, they are already being used.
# Instead pass any needed env variables as build args for the build, which will be available as env variables during the build.
FROM base-image AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ARG API_URL
RUN npm run build


# -- Run the app
FROM base AS production
ENV NODE_ENV=production
WORKDIR /app

# Install runtime dependencies for sharp
RUN apk add --no-cache libc6-compat

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
