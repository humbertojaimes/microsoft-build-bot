// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CognitiveBot.Config;
using CognitiveBot.Helpers;
using CognitiveBot.Model;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CognitiveBot
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class CognitiveBotBot: ActivityHandler, IBot
    {
        private const string WelcomeMessage = "Welcome to Adventure Works";

        private const string InfoMessage = "How can we help you? You can talk to our assistant. Try saying 'I want to access' or 'show me the products list'. If you want to know about a specific product, you can use 'please tell me about mountain bike' or similar messages. Our smart digital assistant will do its best to help you!";

        private const string PatternMessage = @"You can also say help to display some options";

        //private readonly BotService _services;
        //public static readonly string LuisKey = "AdventureWorksBotBot";

        private BotState _conversationState;
        private BotState _userState;

        private readonly CognitiveBotAccessors _accessors;
        private readonly ILogger _logger;
        private readonly EnvironmentConfig _configuration;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="conversationState">The managed conversation state.</param>
        /// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider"/>
        public CognitiveBotBot(ConversationState cs, ILoggerFactory loggerFactory, IOptions<EnvironmentConfig> configuration, 
            //BotService botService,
            ConversationState conversationState, UserState userState)
        {
            //_services = services ?? throw new System.ArgumentNullException(nameof(services));

            this._conversationState = cs;
            _userState = userState;

            _configuration = configuration.Value;
            if (conversationState == null)
            {
                throw new System.ArgumentNullException(nameof(conversationState));
            }

            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _accessors = new CognitiveBotAccessors(conversationState)
            {
                CounterState = conversationState.CreateProperty<CounterState>(CognitiveBotAccessors.CounterStateName),
            };

            _logger = loggerFactory.CreateLogger<CognitiveBotBot>();
            _logger.LogTrace("Turn start.");
        }

        static bool welcome = false;

        public async override Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // aqui se procesan las cards
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var luisAnalysis = await CognitiveBot.Helpers.TextAnalysisHelper.MakeLUISAnalysisAsync(WebUtility.UrlEncode(turnContext.Activity.Text), _configuration.LuisEndPoint, _configuration.LuisAppId);

                if (luisAnalysis.TopScoringIntent.Score > .4)
                {
                    switch (luisAnalysis.TopScoringIntent.Intent)
                    {

                        case Constants.LoginIntent:
                            var ent = luisAnalysis.Entities.FirstOrDefault();

                            switch (ent.Type)
                            {
                                case Constants.EmailLabel:
                                    var email = ent.entity;

                                    var customer = DbHelper.GetCustomer(email, _configuration.DataSource, _configuration.DbUser, _configuration.Password);
                                    var userName = "";

                                    if (customer != null)
                                    {
                                        userName = customer.CustomerName;

                                        var hero = new HeroCard();
                                        hero.Title = "Welcome";
                                        hero.Text = customer.CustomerName;
                                        hero.Subtitle = customer.CompanyName;

                                        var us = _userState.CreateProperty<CustomerShort>(nameof(CustomerShort));
                                        var c = await us.GetAsync(turnContext, () => new CustomerShort());
                                        c.CompanyName = customer.CompanyName;
                                        c.CustomerName = customer.CustomerName;
                                        c.CustomerID = customer.CustomerID;
                                        c.EmailAddress = customer.EmailAddress;

                                        var response = turnContext.Activity.CreateReply();
                                        response.Attachments = new List<Attachment>() { hero.ToAttachment() };
                                        await turnContext.SendActivityAsync(response, cancellationToken);

                                        //await turnContext.SendActivityAsync($"Welcome {userName}");
                                    }
                                    else
                                        await turnContext.SendActivityAsync($"User not found. Pleae try again");
                                    break;
                                default:
                                    await turnContext.SendActivityAsync("Please add your email to your login message");
                                    break;
                            }
                            break;
                        case Constants.ProductInfoIntent:
                            var entity = luisAnalysis.Entities.FirstOrDefault(x => x.Type == Constants.ProductLabel || x.Type == Constants.ProductNameLabel);

                            switch (entity.Type)
                            {
                                case Constants.ProductLabel:
                                case Constants.ProductNameLabel:
                                    var product = "";
                                    var message = "Our Top 5 Products are:";
                                    var productName = entity.entity;

                                    if (entity.Type == Constants.ProductNameLabel)
                                    {
                                        product = productName;
                                        message = "Your query returned the following products: ";
                                    }

                                    var products = DbHelper.GetProducts(product, _configuration.DataSource, _configuration.DbUser, _configuration.Password);
                                    var data = "No results";

                                    var typing = Activity.CreateTypingActivity();
                                    var delay = new Activity { Type = "delay", Value = 5000 };

                                    if (products != null)
                                    {
                                        var responseProducts = turnContext.Activity.CreateReply();
                                        responseProducts.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                        responseProducts.Attachments = new List<Attachment>();

                                        foreach (var item in products)
                                        {
                                            var card = new HeroCard();
                                            card.Subtitle = item.ListPrice.ToString("N2");
                                            card.Title = item.Name;
                                            card.Text = $"{item.Category} - {item.Model} - {item.Color}";
                                            card.Buttons = new List<CardAction>()
                                            {
                                                 new CardAction()
                                                 {
                                                     Value = $"Add product {item.ProductID} to the cart",
                                                     Type = ActionTypes.ImBack,
                                                     Title = " Add To Cart "
                                                 }
                                            };

                                            card.Images = new List<CardImage>()
                                            {
                                                new CardImage()
                                                {


                                                    Url = $"data:image/gif;base64,{item.Photo}"
                                                }
                                            };

                                            var plAttachment = card.ToAttachment();
                                            responseProducts.Attachments.Add(plAttachment);
                                        }

                                        var activities = new IActivity[]
                                        {
                                            typing,
                                            delay,
                                            MessageFactory.Text($"{message}: "),
                                            responseProducts,
                                            MessageFactory.Text("What else can I do for you?")
                                        };

                                        await turnContext.SendActivitiesAsync(activities);
                                    }
                                    else
                                    {
                                        var activities = new IActivity[]
                                        {   typing,
                                            delay,
                                            MessageFactory.Text($"{message}: {data}"),
                                            MessageFactory.Text("What else can I do for you?")
                                        };

                                        await turnContext.SendActivitiesAsync(activities);
                                    }
                                    break;
                                default:
                                    break;
                            }
                            break;


                        case Constants.AddToCartIntent:
                            var entProd = luisAnalysis.Entities.FirstOrDefault();
                            var number = 0;

                            if (entProd.Type == Constants.NumberLabel)
                            {
                                number = int.Parse(entProd.entity);

                                var product = DbHelper.GetProduct(number, _configuration.DataSource, _configuration.DbUser, _configuration.Password);

                                var userStateAccessors = _userState.CreateProperty<CustomerShort>(nameof(CustomerShort));
                                var shoppingCartAccessors = _userState.CreateProperty<List<ShoppingCart>>(nameof(List<ShoppingCart>));

                                var customer = await userStateAccessors.GetAsync(turnContext, () => new CustomerShort());
                                var cart = await shoppingCartAccessors.GetAsync(turnContext, () => new List<ShoppingCart>());

                                var item = new ShoppingCart()
                                {
                                    CustomerID = customer.CustomerID,
                                    ProductID = product.ProductID,
                                    ProductName = product.Name,
                                    ListPrice = product.ListPrice,
                                    Photo = product.Photo
                                };
                                cart.Add(item);

                                var act = new IActivity[]
                                {
                                    Activity.CreateTypingActivity(),
                                    new Activity { Type = "delay", Value = 5000 },
                                    MessageFactory.Text($"Product {product.Name} was added to the cart."),
                                    MessageFactory.Text("What else can I do for you?")
                                };

                                await turnContext.SendActivitiesAsync(act);
                            }

                            break;
                        case Constants.PlaceOrderIntent:
                            //////////////////////77
                            var usAccessors = _userState.CreateProperty<CustomerShort>(nameof(CustomerShort));
                            var scAccessors = _userState.CreateProperty<List<ShoppingCart>>(nameof(List<ShoppingCart>));

                            var cust = await usAccessors.GetAsync(turnContext, () => new CustomerShort());
                            var shoppingCart = await scAccessors.GetAsync(turnContext, () => new List<ShoppingCart>());

                            if (shoppingCart.Count() > 0)
                            {
                                var receipt = turnContext.Activity.CreateReply();
                                receipt.Attachments = new List<Attachment>();

                                var card = new ReceiptCard();
                                card.Title = "Adventure Works";
                                card.Facts = new List<Fact>
                                {
                                    new Fact("Name:", cust.CustomerName),
                                    new Fact("E-mail:", cust.EmailAddress),
                                    new Fact("Company:", cust.CompanyName),
                                };

                                decimal subtotal = 0;
                                decimal p = 16M / 100;

                                card.Items = new List<ReceiptItem>();

                                foreach (var product in shoppingCart)
                                {
                                    var item = new ReceiptItem();
                                    item.Price = product.ListPrice.ToString("N2");
                                    item.Quantity = "1";
                                    item.Text = product.ProductName;
                                    item.Subtitle = product.ProductName;
                                    item.Image = new CardImage()
                                    {
                                        Url = $"data:image/gif;base64, {product.Photo}"
                                    };

                                    subtotal += product.ListPrice;

                                    card.Items.Add(item);
                                    //var plAttachment = card.ToAttachment();
                                    //receipt.Attachments.Add(plAttachment);
                                }
                                receipt.Attachments.Add(card.ToAttachment());

                                var tax = subtotal * p;
                                card.Tax = tax.ToString("N2");

                                var total = subtotal + tax;
                                card.Total = total.ToString("N2");

                                var activities = new IActivity[]
                                {
                                    Activity.CreateTypingActivity(),
                                    new Activity { Type = "delay", Value = 5000 },
                                    MessageFactory.Text("Here is your receipt: "),
                                    receipt,
                                    MessageFactory.Text("What else can I do for you?")
                                };

                                await turnContext.SendActivitiesAsync(activities);
                            }
                            break;
                        default:
                            break;

                    }

                }
                else
                {
                    var text = turnContext.Activity.Text.ToLowerInvariant();
                    switch (text)
                    {
                        case "help":
                            await SendIntroCardAsync(turnContext, cancellationToken);
                            break;
                        default:
                            await turnContext.SendActivityAsync("I did not understand you, sorry. Try again with a different sentence, please", cancellationToken: cancellationToken);
                            break;
                    }

                }
            }

            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (!welcome)
                {
                    welcome = true;

                    await turnContext.SendActivityAsync($"Hi there. {WelcomeMessage}", cancellationToken: cancellationToken);
                    await turnContext.SendActivityAsync(InfoMessage, cancellationToken: cancellationToken);
                    await turnContext.SendActivityAsync(PatternMessage, cancellationToken: cancellationToken);
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} activity detected");
            }

            // Save any state changes that might have occured during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);

        }

        private static async Task SendIntroCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var response = turnContext.Activity.CreateReply();

            var card = new HeroCard();
            card.Title = WelcomeMessage;
            card.Text = InfoMessage;
            card.Images = new List<CardImage>() { new CardImage("https://drive.google.com/uc?id=1eE_WlkW8G9cSI_w9heIWeo53ZkMtQu4x") };
            card.Buttons = new List<CardAction>()
            {
                new CardAction(ActionTypes.OpenUrl, "Enter my credentials", null, "Enter my credentials", "Enter my credentials", "Login"),
                new CardAction(ActionTypes.OpenUrl, "Show me the product list", null, "Show me the product list", "Show me the product list", "ProductInfo"),
            };

            response.Attachments = new List<Attachment>() { card.ToAttachment() };
            await turnContext.SendActivityAsync(response, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Get the state properties from the turn context.
            var conversationStateAccessors = _conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData());

            var userStateAccessors = _userState.CreateProperty<CustomerShort>(nameof(CustomerShort));
            var shoppingCartAccessors = _userState.CreateProperty<List<ShoppingCart>>(nameof(List<ShoppingCart>));

            var customer = await userStateAccessors.GetAsync(turnContext, () => new CustomerShort());
            var cart = await shoppingCartAccessors.GetAsync(turnContext, () => new List<ShoppingCart>());

            if (string.IsNullOrEmpty(customer.CustomerName))
            {
                if (conversationData.PromptedUserForName)
                {
                    customer.CustomerName = turnContext.Activity.Text?.Trim();
                    await turnContext.SendActivityAsync($"Thanks {customer.CustomerName}.");
                    conversationData.PromptedUserForName = false;
                }
                else
                {
                    await turnContext.SendActivityAsync($"What is your name?");
                    conversationData.PromptedUserForName = true;
                }
            }
            else
            {
                var messageTimeOffset = (DateTimeOffset)turnContext.Activity.Timestamp;
                var localMessageTime = messageTimeOffset.ToLocalTime();
                conversationData.Timestamp = localMessageTime.ToString();
                conversationData.ChannelId = turnContext.Activity.ChannelId.ToString();

                // Display state data.
                //await turnContext.SendActivityAsync($"{customer.CustomerName} sent: {turnContext.Activity.Text}");
                //await turnContext.SendActivityAsync($"Message received at: {conversationData.Timestamp}");
                //await turnContext.SendActivityAsync($"Message received from: {conversationData.ChannelId}");
            }
        }


/*
        /// <summary>
        /// Every conversation turn for our Echo Bot will call this method.
        /// There are no dialogs used, since it's "single turn" processing, meaning a single
        /// request and response.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>
        public async Task OnTurnAsync2(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types

            //var activity = turnContext.Activity;
            //activity.ServiceUrl = _configuration.DirectLine;
            //turnContext.Activity.ServiceUrl = _configuration.DirectLine;
            //Console.WriteLine($"Service Url {_configuration.DirectLine}");
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Get the conversation state from the turn context.
                var state = await _accessors.CounterState.GetAsync(turnContext, () => new CounterState());

                // Bump the turn count for this conversation.
                state.TurnCount++;

                // Set the property using the accessor.
                await _accessors.CounterState.SetAsync(turnContext, state);

                // Save the new turn count into the conversation state.
                await _accessors.ConversationState.SaveChangesAsync(turnContext);

                // Echo back to the user whatever they typed. 



                string responseMessage = "Disculpa, no entendí :(";


                var luisAnalysis = await CognitiveBot.Helpers.TextAnalysisHelper.MakeLUISAnalysisAsync(WebUtility.UrlEncode(turnContext.Activity.Text), _configuration.LuisEndPoint, _configuration.LuisAppId);

                if (luisAnalysis.TopScoringIntent.Score > .5)
                    switch (luisAnalysis.TopScoringIntent.Intent)
                    {

                        case "Analizar":


                            IList<RequestMessage> messagesResult = new List<RequestMessage>();

                            foreach (var entity in luisAnalysis.Entities)
                            {
                                RequestMessage requestMessage = new RequestMessage()
                                {
                                    Id = entity.StartIndex.ToString(),
                                    MessageText = entity.entity
                                };
                                messagesResult.Add(requestMessage);
                                Console.WriteLine($"Entity {entity.entity}");
                            }


                            var textAnalysis = await Helpers.TextAnalysisHelper.MakeKeywordAnalysisAsync(messagesResult, _configuration.TextEndPoint);

                            var res = string.Join(" | ", textAnalysis.ResultMessages?.First()?.KeyPhrases?.Take(5));
                            responseMessage = res;
                            break;

                        case "ObtenerProductos":
                            List<Product> products = new List<Product>();
                            try
                            {
                                products = Helpers.DbHelper.GetProducts(_configuration.DataSource, _configuration.DbUser, _configuration.Password);
                            }
                            catch
                            {

                            }
                            StringBuilder builder = new StringBuilder();
                            builder.AppendLine($"Hay {products.Count}");

                            foreach (var product in products)
                            {
                                builder.AppendLine($"{product.Name} - {product.Quantity}");
                            }
                            responseMessage = builder.ToString();
                            break;

                        default:
                            responseMessage = $"Turn {state.TurnCount}: You sent '{turnContext.Activity.Text}'\n";
                            break;
                    }





                await turnContext.SendActivityAsync(responseMessage);
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }
        }

    */
    }
}
