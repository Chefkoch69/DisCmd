using Discord;
using Discord.Gateway;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace command_line
{
    class Program
    {
        #region login & input 

        static void Main(string[] args)
        {
            logo();
            string token = File.ReadAllText("token.txt");
            if (token.Replace(" ", "") == "")
            {
                Console.WriteLine(" Token ");
                Console.Write($" [{Environment.UserName}@Anarchy ~ ]  $ ");
                using (StreamWriter writer = new StreamWriter("token.txt"))
                {
                    writer.WriteLine(Console.ReadLine().Replace(" ", ""));
                    token = File.ReadAllText("token.txt");
                }
            }
            DiscordClient clieynt = new DiscordClient(token);
            DiscordSocketClient client = new DiscordSocketClient();
            client.Login(token);

            client.OnLoggedIn += Client_OnLoggedIn;
            Thread.Sleep(-1);
        }

        private static void Client_OnLoggedIn(DiscordSocketClient client, LoginEventArgs args)
        {
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($" [{Environment.UserName}@Anarchy ~ ]  $ ");
                string input = Console.ReadLine();

                commandhandler(input, client);
            }
        }
        #endregion
        public static void logo()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(@"

    ██      ▄   ██   █▄▄▄▄ ▄█▄     ▄  █ ▀▄    ▄ 
    █ █      █  █ █  █  ▄▀ █▀ ▀▄  █   █   █  █  
    █▄▄█ ██   █ █▄▄█ █▀▀▌  █   ▀  ██▀▀█    ▀█   
    █  █ █ █  █ █  █ █  █  █▄  ▄▀ █   █    █    
       █ █  █ █    █   █   ▀███▀     █   ▄▀     
      █  █   ██   █   ▀             ▀           
     ▀           ▀                              

");
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        static void commandhandler(string input, DiscordSocketClient client)
        {
            //action = lowered, without spaces
            //args = arguments
            #region splitting input
            string action = input.Replace(" ", "").ToLower();
            string[] args = input.Replace(" ", ";").Split(";".ToCharArray());
            #endregion
            #region help & clear
            switch (input)
            {
                #region help
                case "help":
                    help();
                    break;
                #endregion
                case "clear":
                    Console.Clear();
                    logo();
                    break;
            }
            #endregion
            #region send
            if (input.Contains("send"))
            {
                try
                {
                    ulong ye = Convert.ToUInt64(args[1]);

                    Console.Write($" [Message@Anarchy ~ ]  $ ");
                    string message = Console.ReadLine();

                    client.SendMessage(ye, message);
                }
                catch (IndexOutOfRangeException) { Console.WriteLine(@"
Syntax : send (channel (ID))
               ^^^^^^^^^^^^ 
               "); }
                catch { }
            }

            #endregion
            #region join
            if (input.Contains("join"))
            {
                string[] array = input.Replace(" ", ";").Split(";".ToCharArray());

                try
                {
                    string inv = array[1].Replace("https://discord.gg/", "").Replace("discord.gg/", "");
                    client.JoinGuild(inv);
                }
                catch (IndexOutOfRangeException) { Console.WriteLine(@"
Syntax : Join (invite)
               ^^^^^^"); }
            }
            #endregion
            #region leave
            if (input.Contains("leave"))
            {
                try
                {
                    ulong yea = Convert.ToUInt64(args[1]);
                    client.LeaveGuild(yea);
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine(@"
Syntax : leave (guild (ID))
               ^^^^^^^^^^^^
");
                }
                catch { }
            }
            #endregion
            #region friend
            if (input.Contains("friend"))
            {
                try
                {
                    ulong y = Convert.ToUInt64(args[1]);
                    client.SendFriendRequest(y);
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine(@"

Syntax : friend (user (ID))
                ^^^^^^^^^^^

");
                }
                catch { }
            }
            #endregion
            #region dupe
            if (input.Contains("dupe"))
            {
                try
                {
                    SocketGuild guild = client.GetCachedGuild(ulong.Parse(args[1])); // u could also just grab from args.Guilds, but i prefer this method bcuz we can be sure that the info is up to date

                    Console.WriteLine(" -> Duplicating Guild");
                    DiscordGuild ourGuild = client.CreateGuild(guild.Name, guild.Icon == null ? null : guild.Icon.Download(), guild.Region);
                    ourGuild.Modify(new GuildProperties() { DefaultNotifications = guild.DefaultNotifications, VerificationLevel = guild.VerificationLevel });

                    Console.WriteLine(" -> Duplicating roles");
                    Dictionary<ulong, ulong> dupedRoles = new Dictionary<ulong, ulong>();
                    foreach (var role in guild.Roles.OrderBy(r => r.Position).Reverse())
                        dupedRoles.Add(role.Id, ourGuild.CreateRole(new RoleProperties() { Name = role.Name, Color = role.Color, Mentionable = role.Mentionable, Permissions = role.Permissions, Seperated = role.Seperated }).Id);

                    Console.WriteLine(" -> Duplicating emojis");
                    foreach (var emoji in guild.Emojis)
                    {
                        try
                        {
                            ourGuild.CreateEmoji(new EmojiProperties() { Name = emoji.Name, Image = emoji.Icon.Download() });
                        }
                        catch (DiscordHttpException ex)
                        {
                            break;
                        }
                        Console.WriteLine(" -> Removing default channels");
                        foreach (var channel in ourGuild.GetChannels())
                            channel.Delete();

                        Console.WriteLine(" -> Duplicating channels");
                        Dictionary<ulong, ulong> dupedCategories = new Dictionary<ulong, ulong>();
                        foreach (var channel in guild.Channels.OrderBy(c => c.Type != ChannelType.Category))
                        {
                            var ourChannel = ourGuild.CreateChannel(channel.Name, channel.IsText ? ChannelType.Text : channel.Type, channel.ParentId.HasValue ? dupedCategories[channel.ParentId.Value] : (ulong?)null);
                            ourChannel.Modify(new GuildChannelProperties() { Position = channel.Position });

                            if (ourChannel.Type == ChannelType.Text)
                            {
                                var channelAsText = (TextChannel)channel;

                                ((TextChannel)ourChannel).Modify(new TextChannelProperties() { Nsfw = channelAsText.Nsfw, SlowMode = channelAsText.SlowMode, Topic = channelAsText.Topic });
                            }
                            else if (ourChannel.Type == ChannelType.Voice)
                            {
                                var channelAsVoice = (VoiceChannel)channel;

                                ((VoiceChannel)ourChannel).Modify(new VoiceChannelProperties() { Bitrate = Math.Max(96000, channelAsVoice.Bitrate), UserLimit = channelAsVoice.UserLimit });
                            }

                            foreach (var overwrite in channel.PermissionOverwrites)
                            {
                                if (overwrite.Type == PermissionOverwriteType.Role)
                                    ourChannel.AddPermissionOverwrite(dupedRoles[overwrite.AffectedId], PermissionOverwriteType.Role, overwrite.Allow, overwrite.Deny);
                            }

                            if (ourChannel.Type == ChannelType.Category)
                                dupedCategories.Add(channel.Id, ourChannel.Id);
                        }

                        Console.WriteLine(" -> Done");
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine(@"
Syntax : dupe (guild(ID))
              ^^^^^^^^^^

");
                }
                catch { }

            }
            #endregion
            #region guilds
            if (input.Contains("guilds"))
            {
                foreach (var guild in client.GetGuilds())
                {
                    Thread.Sleep(1000);
                    Console.WriteLine($@"
 - {guild.Name}
  - ID - {guild.Id}
  
");
                }
            }
            #endregion
            #region channels
            if (input.Contains("channels"))
            {
                try
                {
                    foreach (var channel in client.GetGuildChannels(Convert.ToUInt64(args[1])))
                    {
                        Thread.Sleep(1000);
                        Console.WriteLine($@"
 - {channel.Name}
  - ID -{channel.Id}

");
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine(@"
Syntax : channels (Guild (ID))
                  ^^^^^^^^^^^

");
                }

            }
            #endregion



        }
        public static void help()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            #region Help Command

            Console.WriteLine(@"
 - help -
 - clear - 

 - join (Invite) -
 - leave (guild (ID)) -

 - send (channel (ID)) -

 - friend (user (ID)) -

 - channels (guild (ID)) -
 - guilds -
 ");


            #endregion
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

    }
}
