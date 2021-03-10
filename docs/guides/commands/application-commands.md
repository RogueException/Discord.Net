# Application commands

Application commands are a new feature thats still a work in progress, this guide will show you how to make the best of em.




## Getting started

### Configuring

There is a new configuration setting for your DiscordSocketClient called `AlwaysAcknowledgeInteractions`, It's default value is true. 
Interactions work off of the Recieve -> Respond pipeline, meaning if you dont acknowledge the interaction within 3 seconds its gone forever.
With `AlwaysAcknowledgeInteractions` set to true, the client will automatically acknowledge the interaction as its recieved, 
letting you wait up to 15 minutes before responding with a message.

With `AlwaysAcknowledgeInteractions` set to false you will have to acknowledge the interaction yourself via the `InteractionCreated` event

### Registering commands

While there is no "easy" way to register command right now, in the future I plan to write a command service to help with that, but right now you have to use the rest
client to create your command:

```cs
_client.Ready += RegisterCommands

...

private async Task RegisterCommands()
{
  // Creating a global command
  var myGlobalCommand = await _client.Rest.CreateGlobalCommand(new Discord.SlashCommandCreationProperties()
  {
    Name = "example",
    Description = "Runs the example command",
    Options = new List<Discord.ApplicationCommandOptionProperties>()
    {
      new ApplicationCommandOptionProperties()
      {
        Name = "Example option",
        Required = false,
        Description = "Option Description",
        Type = Discord.ApplicationCommandOptionType.String,
      }
    }
  });

  // Creating a guild command
  var myGuildCommand = await _client.Rest.CreateGuildCommand(new Discord.SlashCommandCreationProperties()
  {
    Name = "guildExample",
    Description = "Runs the guild example command",
    Options = new List<Discord.ApplicationCommandOptionProperties>()
    {
      new ApplicationCommandOptionProperties()
      {
        Name = "Guild example option",
        Required = false,
        Description = "Guild option description",
        Type = Discord.ApplicationCommandOptionType.String,
      }
    }
  }, 1234567890); // <- the guild id
}
```
CreateGuildCommand returns a `RestGuildCommand` class which can be used to modify/delete your command on the fly, it also contains details about your command.
CreateGlobalCOmmand returns a `RestGlobalCommand` class which can be used to modify/delete your command on the fly, it also contains details about your command.

### Getting a list of all your commands
You can fetch a list of all your global commands via rest:
```cs
var commands = _client.Rest.GetGlobalApplicationCommands();
```
This returns a `IReadOnlyCollection<RestGlobalCommand>`.

You can also fetch guild specific commands:
```cs
var commands = _client.Rest.GetGuildApplicationCommands(1234567890) 
```
This returns all the application commands in that guild.

### Responding

First thing we want to do is listen to the `InteractionCreated` event. This event is fired when a socket interaction is recieved via the gateway, It looks somthing like this
```cs
_client.InteractionCreated += MyEventHandler;

...

private async Task MyEventHandler(SocketInteraction arg)
{
  // handle the interaction here
}
```

A socket interaction is made up of these properties and methods:

|  Name  |  Description |
|--------|--------------|
| Guild  | The `SocketGuild` this interaction was used in |
| Channel | The `SocketTextChannel` this interaction was used in |
| Member | The `SocketGuildUser` that executed the interaction |
| Type | The [InteractionType](https://discord.com/developers/docs/interactions/slash-commands#interaction-interactiontype) of this interaction |
| Data | The `SocketInteractionData` associated with this interaction | 
| Token | The token used to respond to this interaction | 
| Version | The version of this interaction |
| CreatedAt | The time this interaction was created |
| IsValidToken | Whether or not the token to respond to this interaction is still valid |
| RespondAsync | Responds to the interaction | 
| FollowupAsync | Sends a followup message to the interaction |



#### Whats the difference between `FollowupAsync` and `RespondAsync`?
RespondAsync is the initial responce to the interaction, its used to "capture" the interaction, while followup is used to send more messages to the interaction.
Basically, you want to first use `RespondAsync` to acknowledge the interaction, then if you need to send anything else regarding that interaction you would use `FollowupAsync`
If you have `AlwaysAcknowledgeInteractions` set to true in your client config then it will automatically acknowledge the interaction without sending a message, 
in this case you can use either or to respond.

#### Example ping pong command
```cs
_client.InteractionCreated += MyEventHandler;
_client.Ready += CreateCommands

...

private async Task CreateCommands()
{
  await _client.Rest.CreateGlobalCommand(new Discord.SlashCommandCreationProperties()
  {
    Name = "ping",
    Description = "ping for a pong!",
  });
}

private async Task MyEventHandler(SocketInteraction arg)
{
  switch(arg.Type) // We want to check the type of this interaction
  {
    case InteractionType.ApplicationCommand: // If it is a command
      await MySlashCommandHandler(arg); // Handle the command somewhere
      break;
     default: // We dont support it
      Console.WriteLine("Unsupported interaction type: " + arg.Type);
      break;
  }
}

private async Task MySlashCommandHandler(SocketInteraction arg)
{
  switch(arg.Name)
  {
    case "ping":
      await arg.RespondAsync("Pong!");
      break;
  }
}
```

#### Example hug command
```cs
_client.InteractionCreated += MyEventHandler;
_client.Ready += CreateCommands;

...

private async Task CreateCommands()
{
  await _client.Rest.CreateGlobalCommand(new Discord.SlashCommandCreationProperties()
  {
    Name = "hug",
    Description = "Hugs a user!",
    Options = new List<Discord.ApplicationCommandOptionProperties>()
    {
      new ApplicationCommandOptionProperties()
      {
        Name = "User",
        Required = true,
        Description = "The user to hug",
        Type = Discord.ApplicationCommandOptionType.User,
      }
    }
  });
}

private async Task MyEventHandler(SocketInteraction arg)
{
  switch(arg.Type) // We want to check the type of this interaction
  {
    case InteractionType.ApplicationCommand: // If it is a command
      await MySlashCommandHandler(arg); // Handle the command somewhere
      break;
     default: // We dont support it
      Console.WriteLine("Unsupported interaction type: " + arg.Type);
      break;
  }
}

private async Task MySlashCommandHandler(SocketInteraction arg)
{
  switch(arg.Name)
  {
    case "hug":
      // Get the user argument
      var option = arg.Data.Options.First(x => x.Name == "user");
      // We know that the options value must be a user
      if(option.Value is SocketGuildUser user)
      {
        await arg.RespondAsync($"Hugged {user.Mention}");
      }
      break;
  }
}
```


