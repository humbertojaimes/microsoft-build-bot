const directline = require("offline-directline/bridge");
const express = require("express");

const app = express();
directline.initializeRoutes(app, process.env.UseDirectLineHost, process.env.UseDirectLinePort, "http://" + process.env.BotEndPoint + process.env.BotPath);
//directline.initializeRoutes(app, "http://directline",3000, "http://127.0.0.1:3978/api/messages");