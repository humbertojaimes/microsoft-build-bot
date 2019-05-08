"use strict";
var __assign = (this && this.__assign) || function () {
    __assign = Object.assign || function(t) {
        for (var s, i = 1, n = arguments.length; i < n; i++) {
            s = arguments[i];
            for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p))
                t[p] = s[p];
        }
        return t;
    };
    return __assign.apply(this, arguments);
};
exports.__esModule = true;
var bodyParser = require("body-parser");
var express = require("express");
var fetch = require("isomorphic-fetch");
var moment = require("moment");
var uuidv4 = require("uuid/v4");
var expiresIn = 1800;
var conversationsCleanupInterval = 10000;
var conversations = {};
var botDataStore = {};
exports.getRouter = function (serviceUrl, botUrl, conversationInitRequired) {
    if (conversationInitRequired === void 0) { conversationInitRequired = true; }
    var router = express.Router();
    router.use(bodyParser.json()); // for parsing application/json
    router.use(bodyParser.urlencoded({ extended: true })); // for parsing application/x-www-form-urlencoded
    router.use(function (req, res, next) {
        res.header('Access-Control-Allow-Origin', '*');
        res.header('Access-Control-Allow-Methods', 'GET, PUT, POST, DELETE, PATCH, OPTIONS');
        res.header('Access-Control-Allow-Headers', 'Origin, X-Requested-With, Content-Type, Accept, Authorization, x-ms-bot-agent');
        next();
    });
    // CLIENT ENDPOINT
    router.options('/directline', function (req, res) {
        res.status(200).end();
    });
    // Creates a conversation
    router.post('/directline/conversations', function (req, res) {
        var conversationId = uuidv4().toString();
        conversations[conversationId] = {
            conversationId: conversationId,
            history: []
        };
        console.log('Created conversation with conversationId: ' + conversationId);
        var activity = createConversationUpdateActivity(serviceUrl, conversationId);
        fetch(botUrl, {
            method: 'POST',
            body: JSON.stringify(activity),
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(function (response) {
            res.status(response.status).send({
                conversationId: conversationId,
                expiresIn: expiresIn
            });
        });
    });
    // Reconnect API
    router.get('/v3/directline/conversations/:conversationId', function (req, res) { console.warn('/v3/directline/conversations/:conversationId not implemented'); });
    // Gets activities from store (local history array for now)
    router.get('/directline/conversations/:conversationId/activities', function (req, res) {
        var watermark = req.query.watermark && req.query.watermark !== 'null' ? Number(req.query.watermark) : 0;
        var conversation = getConversation(req.params.conversationId, conversationInitRequired);
        if (conversation) {
            // If the bot has pushed anything into the history array
            if (conversation.history.length > watermark) {
                var activities = conversation.history.slice(watermark);
                res.status(200).json({
                    activities: activities,
                    watermark: watermark + activities.length
                });
            }
            else {
                res.status(200).send({
                    activities: [],
                    watermark: watermark
                });
            }
        }
        else {
            // Conversation was never initialized
            res.status(400).send();
        }
    });
    // Sends message to bot. Assumes message activities
    router.post('/directline/conversations/:conversationId/activities', function (req, res) {
        var incomingActivity = req.body;
        // Make copy of activity. Add required fields
        var activity = createMessageActivity(incomingActivity, serviceUrl, req.params.conversationId);
        var conversation = getConversation(req.params.conversationId, conversationInitRequired);
        if (conversation) {
            conversation.history.push(activity);
            fetch(botUrl, {
                method: 'POST',
                body: JSON.stringify(activity),
                headers: {
                    'Content-Type': 'application/json'
                }
            }).then(function (response) {
                res.status(response.status).json({ id: activity.id });
            });
        }
        else {
            // Conversation was never initialized
            res.status(400).send();
        }
    });
    router.post('/v3/directline/conversations/:conversationId/upload', function (req, res) { console.warn('/v3/directline/conversations/:conversationId/upload not implemented'); });
    router.get('/v3/directline/conversations/:conversationId/stream', function (req, res) { console.warn('/v3/directline/conversations/:conversationId/stream not implemented'); });
    // BOT CONVERSATION ENDPOINT
    router.post('/v3/conversations', function (req, res) { console.warn('/v3/conversations not implemented'); });
    router.post('/v3/conversations/:conversationId/activities', function (req, res) {
        var activity;
        activity = req.body;
        activity.id = uuidv4();
        activity.from = { id: 'id', name: 'Bot' };
        var conversation = getConversation(req.params.conversationId, conversationInitRequired);
        if (conversation) {
            conversation.history.push(activity);
            res.status(200).send();
        }
        else {
            // Conversation was never initialized
            res.status(400).send();
        }
    });
    router.post('/v3/conversations/:conversationId/activities/:activityId', function (req, res) {
        var activity;
        activity = req.body;
        activity.id = uuidv4();
        activity.from = { id: 'id', name: 'Bot' };
        var conversation = getConversation(req.params.conversationId, conversationInitRequired);
        if (conversation) {
            conversation.history.push(activity);
            res.status(200).send();
        }
        else {
            // Conversation was never initialized
            res.status(400).send();
        }
    });
    router.get('/v3/conversations/:conversationId/members', function (req, res) { console.warn('/v3/conversations/:conversationId/members not implemented'); });
    router.get('/v3/conversations/:conversationId/activities/:activityId/members', function (req, res) { console.warn('/v3/conversations/:conversationId/activities/:activityId/members'); });
    // BOTSTATE ENDPOINT
    router.get('/v3/botstate/:channelId/users/:userId', function (req, res) {
        console.log('Called GET user data');
        getBotData(req, res);
    });
    router.get('/v3/botstate/:channelId/conversations/:conversationId', function (req, res) {
        console.log(('Called GET conversation data'));
        getBotData(req, res);
    });
    router.get('/v3/botstate/:channelId/conversations/:conversationId/users/:userId', function (req, res) {
        console.log('Called GET private conversation data');
        getBotData(req, res);
    });
    router.post('/v3/botstate/:channelId/users/:userId', function (req, res) {
        console.log('Called POST setUserData');
        setUserData(req, res);
    });
    router.post('/v3/botstate/:channelId/conversations/:conversationId', function (req, res) {
        console.log('Called POST setConversationData');
        setConversationData(req, res);
    });
    router.post('/v3/botstate/:channelId/conversations/:conversationId/users/:userId', function (req, res) {
        setPrivateConversationData(req, res);
    });
    router["delete"]('/v3/botstate/:channelId/users/:userId', function (req, res) {
        console.log('Called DELETE deleteStateForUser');
        deleteStateForUser(req, res);
    });
    return router;
};
/**
 * @param app The express app where your offline-directline endpoint will live
 * @param port The port where your offline-directline will be hosted
 * @param botUrl The url of the bot (e.g. http://127.0.0.1:3978/api/messages)
 * @param conversationInitRequired Requires that a conversation is initialized before it is accessed, returning a 400
 * when not the case. If set to false, a new conversation reference is created on the fly. This is true by default.
 */
exports.initializeRoutes = function (app, host, port, botUrl, conversationInitRequired) {
    if (port === void 0) { port = 3000; }
    if (conversationInitRequired === void 0) { conversationInitRequired = true; }
    conversationsCleanup();
    var directLineEndpoint = host + ":" + port;
    var router = exports.getRouter(directLineEndpoint, botUrl, conversationInitRequired);
    app.use(router);
    app.listen(port, function () {
        console.log("Listening for messages from client on " + directLineEndpoint);
        console.log("Routing messages to bot on " + botUrl);
    });
};
var getConversation = function (conversationId, conversationInitRequired) {
    // Create conversation on the fly when needed and init not required
    if (!conversations[conversationId] && !conversationInitRequired) {
        conversations[conversationId] = {
            conversationId: conversationId,
            history: []
        };
    }
    return conversations[conversationId];
};
var getBotDataKey = function (channelId, conversationId, userId) {
    return "$" + (channelId || '*') + "!" + (conversationId || '*') + "!" + (userId || '*');
};
var setBotData = function (channelId, conversationId, userId, incomingData) {
    var key = getBotDataKey(channelId, conversationId, userId);
    var newData = {
        eTag: new Date().getTime().toString(),
        data: incomingData.data
    };
    if (incomingData) {
        botDataStore[key] = newData;
    }
    else {
        delete botDataStore[key];
        newData.eTag = '*';
    }
    return newData;
};
var getBotData = function (req, res) {
    var key = getBotDataKey(req.params.channelId, req.params.conversationId, req.params.userId);
    console.log('Data key: ' + key);
    res.status(200).send(botDataStore[key] || { data: null, eTag: '*' });
};
var setUserData = function (req, res) {
    res.status(200).send(setBotData(req.params.channelId, req.params.conversationId, req.params.userId, req.body));
};
var setConversationData = function (req, res) {
    res.status(200).send(setBotData(req.params.channelId, req.params.conversationId, req.params.userId, req.body));
};
var setPrivateConversationData = function (req, res) {
    res.status(200).send(setBotData(req.params.channelId, req.params.conversationId, req.params.userId, req.body));
};
var deleteStateForUser = function (req, res) {
    Object.keys(botDataStore)
        .forEach(function (key) {
        if (key.endsWith("!{req.query.userId}")) {
            delete botDataStore[key];
        }
    });
    res.status(200).send();
};
// CLIENT ENDPOINT HELPERS
var createMessageActivity = function (incomingActivity, serviceUrl, conversationId) {
    return __assign({}, incomingActivity, { channelId: 'emulator', serviceUrl: serviceUrl, conversation: { id: conversationId }, id: uuidv4() });
};
var createConversationUpdateActivity = function (serviceUrl, conversationId) {
    var activity = {
        type: 'conversationUpdate',
        channelId: 'emulator',
        serviceUrl: serviceUrl,
        conversation: { id: conversationId },
        id: uuidv4(),
        membersAdded: [],
        membersRemoved: [],
        from: { id: 'offline-directline', name: 'Offline Directline Server' }
    };
    return activity;
};
var conversationsCleanup = function () {
    setInterval(function () {
        var expiresTime = moment().subtract(expiresIn, 'seconds');
        Object.keys(conversations).forEach(function (conversationId) {
            if (conversations[conversationId].history.length > 0) {
                var lastTime = moment(conversations[conversationId].history[conversations[conversationId].history.length - 1].localTimestamp);
                if (lastTime < expiresTime) {
                    delete conversations[conversationId];
                    console.log('deleted cId: ' + conversationId);
                }
            }
        });
    }, conversationsCleanupInterval);
};
