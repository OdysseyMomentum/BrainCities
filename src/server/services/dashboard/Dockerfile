FROM node:10

ARG NODE_ENV=development
ENV NODE_ENV=${NODE_ENV}

# Create app directory
WORKDIR /usr/src/app

# Install app dependencies
# A wildcard is used to ensure both package.json AND package-lock.json are copied
# where available (npm@5+)
COPY services/dashboard/package*.json ./

RUN mkdir -p ./temp/apiuploads

RUN npm install
# If you are building your code for production
# RUN npm ci --only=production

# Bundle app source
COPY services/dashboard/. .

EXPOSE 8000

CMD [ "npm", "start" ]
