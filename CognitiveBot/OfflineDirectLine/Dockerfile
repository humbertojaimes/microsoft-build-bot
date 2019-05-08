FROM node:10.15.3

ARG UseDirectLinePort
ENV UseDirectLinePort=${UseDirectLinePort} 
ARG BotEndPoint
ENV BotEndPoint=${BotEndPoint} 
ARG BotPath
ENV BotPath=${BotPath} 
ARG UseDirectLineHost
ENV UseDirectLineHost=${UseDirectLineHost}


WORKDIR /usr/src/app
COPY OfflineDirectLine/package*.json ./
RUN npm install

COPY OfflineDirectLine/. .
RUN ls -la .


EXPOSE 3010
EXPOSE 9222 

CMD [ "npm", "start", "debug" ]