FROM apify/actor-node-playwright-chrome:24-1.63.0
COPY --chown=myuser package*.json ./
RUN npm install --omit=dev --audit=false
COPY --chown=myuser src ./src
ENV PORT=3050
EXPOSE 3050
CMD ["node", "src/index.js"]