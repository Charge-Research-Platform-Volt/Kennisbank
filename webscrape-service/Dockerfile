FROM node:24-slim
WORKDIR /app
COPY package*.json ./
RUN npm install --omit=dev --audit=false
# Install chromium and clean up after to slim down package
RUN npx playwright install --with-deps --only-shell chromium \
    && rm -rf /var/lib/apt/lists/* /var/cache/apt/archives/*
COPY src ./src
ENV PORT=3050
EXPOSE 3050
CMD ["node", "src/index.js"]