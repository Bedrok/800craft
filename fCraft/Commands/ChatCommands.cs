﻿// Copyright 2009, 2010, 2011, 2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using fCraft.Events;

namespace fCraft
{
    static class ChatCommands
    {

        public static void Init()
        {
            CommandManager.RegisterCommand(CdSay);
            CommandManager.RegisterCommand(CdStaff);
            CommandManager.RegisterCommand(CdIgnore);
            CommandManager.RegisterCommand(CdUnignore);
            CommandManager.RegisterCommand(CdMe);
            CommandManager.RegisterCommand(CdRoll);
            CommandManager.RegisterCommand(CdDeafen);
            CommandManager.RegisterCommand(CdClear);
            CommandManager.RegisterCommand(CdTimer);
            CommandManager.RegisterCommand(cdReview);
            CommandManager.RegisterCommand(CdAdminChat);
            CommandManager.RegisterCommand(CdCustomChat);
            CommandManager.RegisterCommand(cdAway);
            CommandManager.RegisterCommand(cdHigh5);
            CommandManager.RegisterCommand(CdPoke);
            CommandManager.RegisterCommand(CdTroll);
            CommandManager.RegisterCommand(CdVote);
            CommandManager.RegisterCommand(CdBroMode);
            CommandManager.RegisterCommand(CdRageQuit);
            CommandManager.RegisterCommand(CdQuit);

            Player.Moved += new EventHandler<Events.PlayerMovedEventArgs>(Player_IsBack);
        }

        static readonly CommandDescriptor CdQuit = new CommandDescriptor
        {
            Name = "Quitmsg",
            Aliases = new[] { "quit", "quitmessage" },
            Category = CommandCategory.Chat,
            IsConsoleSafe = false,
            Permissions = new[] { Permission.Chat },
            Usage = "/Quitmsg [message]",
            Help = "Adds a farewell message which is displayed when you leave the server.",
            Handler = QuitHandler
        };

        static void QuitHandler(Player player, Command cmd)
        {
            string Msg = cmd.NextAll();

            if (Msg.Length < 1)
            {
                CdQuit.PrintUsage(player);
                return;
            }

            else
            {
                player.Info.LeaveMsg = "left the server: &C" + Msg;
                player.Message("Your quit message is now set to: {0}", Msg);
            }
        }

        static readonly CommandDescriptor CdRageQuit = new CommandDescriptor
        {
            Name = "Ragequit",
            Aliases = new[] { "rq" },
            Category = CommandCategory.Chat,
            IsConsoleSafe = false,
            Permissions = new[] { Permission.RageQuit },
            Usage = "/Ragequit [reason]",
            Help = "An anger-quenching way to leave the server.",
            Handler = RageHandler
        };

        static void RageHandler(Player player, Command cmd)
        {
            string reason = cmd.NextAll();

            if (reason.Length < 1)
            {
                Server.Players.Message("{0} &4RageQuit from the server", player.ClassyName);
                player.Kick(Player.Console, "RageQuit", LeaveReason.RageQuit, false, false, false);
                return;
            }

            else
            {
                Server.Players.Message("{0} &1RageQuit from the server: &C{1}",
                                player.ClassyName, reason);
                player.Kick(Player.Console, reason, LeaveReason.RageQuit, false, false, false);
            }
        }

        static readonly CommandDescriptor CdBroMode = new CommandDescriptor
        {
            Name = "Bromode",
            Aliases = new string[] { "bm" },
            Category = CommandCategory.Chat,
            Permissions = new[] { Permission.BroMode },
            IsConsoleSafe = true,
            Usage = "/Bromode",
            Help = "Toggles bromode.",
            Handler = BroMode
        };

        static void BroMode(Player player, Command command)
        {
            if (!fCraft.Utils.BroMode.Active)
            {
                foreach (Player p in Server.Players)
                {
                    fCraft.Utils.BroMode.GetInstance().RegisterPlayer(p);
                }
                fCraft.Utils.BroMode.Active = true;
                Server.Players.Message("{0}&S turned Bro mode on.", player.Info.Rank.Color + player.Name);
            }
            else
            {
                foreach (Player p in Server.Players)
                {
                    fCraft.Utils.BroMode.GetInstance().UnregisterPlayer(p);
                }

                fCraft.Utils.BroMode.Active = false;
                Server.Players.Message("{0}&S turned Bro Mode off.", player.Info.Rank.Color + player.Name);
            }
        }

        public static void Player_IsBack(object sender, Events.PlayerMovedEventArgs e)
        {
            if (e.Player.IsAway)
            {
                // We need to have block positions, so we divide by 32
                Vector3I oldPos = new Vector3I(e.OldPosition.X / 32, e.OldPosition.Y / 32, e.OldPosition.Z / 32);
                Vector3I newPos = new Vector3I(e.NewPosition.X / 32, e.NewPosition.Y / 32, e.NewPosition.Z / 32);

                // Check if the player actually moved and not just rotated
                if ((oldPos.X != newPos.X) || (oldPos.Y != newPos.Y) || (oldPos.Z != newPos.Z))
                {
                    Server.Players.Message("{0} &Eis no longer away", e.Player.ClassyName);
                    e.Player.IsAway = false;
                }
            }
        }

        static readonly CommandDescriptor CdVote = new CommandDescriptor
       {
           Name = "Vote",
           Category = CommandCategory.Chat,
           Permissions = new[] { Permission.Chat },
           IsConsoleSafe = false,
           NotRepeatable = true,
           Usage = "/Vote | Ask | Kick | Yes | No | Abort",
           Help = "Creates a server-wide vote.",
           Handler = VoteHandler
       };

        static void VoteHandler(Player player, Command cmd)
        {
            string option = cmd.Next();
            switch (option)
            {
                default:
                    if (Server.VoteIsOn)
                    {
                        if (Server.VoteKickReason == null)
                        {
                            player.Message("{0}&C asked: {1}", Server.VoteAskStarter, Server.Question);
                            CdVote.PrintUsage(player);
                            return;
                        }
                        else
                            player.Message("&CA VoteKick has started for {0}&C, reason: {1}", Server.TargetName, Server.VoteKickReason);
                        CdVote.PrintUsage(player);
                        return;
                    }
                    else
                        CdVote.PrintUsage(player);
                    break;

                case "abort":
                case "stop":
                    if (!Server.VoteIsOn)
                    {
                        player.Message("No vote is currently running");
                        return;
                    }

                    if (!player.Can(Permission.MakeVotes))
                    {
                        player.Message("You do not have Permission to abort votes");
                        return;
                    }
                    Server.VoteYes = 0;
                    Server.VoteNo = 0;
                    Server.VoteIsOn = false;
                    Scheduler.NewTask(t => VoteKickCheck(player)).Stop();
                    Scheduler.NewTask(t => VoteKickCheck(player)).IsStopped = true;
                    Scheduler.NewTask(t => VoteCheck(player)).Stop();
                    Scheduler.NewTask(t => VoteCheck(player)).IsStopped = true;
                    Scheduler.UpdateCache();

                    foreach (Player V in Server.Voted)
                    {
                        V.Info.HasVoted = false;
                        V.Message("Your vote was cancelled");
                    }

                    Server.Players.Message("{0} &Saborted the vote.", player.ClassyName);
                    break;

                case "yes":
                    if (!Server.VoteIsOn)
                    {
                        player.Message("No vote is currently running");
                        return;
                    }

                    if (player.Info.HasVoted)
                    {
                        player.Message("&CYou have already voted");
                        return;
                    }
                    Server.Voted.Add(player);
                    Server.VoteYes++;
                    player.Info.HasVoted = true;
                    player.Message("&8You have voted for 'Yes'");
                    break;

                case "kick":
                    string ToKick = cmd.Next();
                    string reason = cmd.NextAll();

                    if (ToKick == null)
                    {
                        CdVote.PrintUsage(player);
                        return;
                    }

                    Player target = Server.FindPlayerOrPrintMatches(player, ToKick, false, true);

                    if (!player.Can(Permission.MakeVoteKicks))
                    {
                        player.Message("You do not have permissions to start a VoteKick");
                        return;
                    }
                    if (Server.VoteIsOn)
                    {
                        player.Message("A vote has already started. Each vote lasts 1 minute.");
                        return;
                    }
                    if (reason.Length < 3)
                    {
                        player.Message("Invalid reason");
                        return;
                    }
                    if (target == null)
                        return;
                    /*if (target == player)
                    {
                        player.Message("You cannot VoteKick yourself, lol");
                        return;
                    }*/
                    if (!Player.IsValidName(ToKick))
                    {
                        player.Message("Invalid name");
                        return;
                    }
                    Server.Players.Message("{0}&S started a VoteKick for player: {1}", player.ClassyName, target.ClassyName);
                    Server.Players.Message("&WReason: {0}", reason);
                    Server.Players.Message("&9Vote now! &S/Vote &AYes &Sor /Vote &CNo");
                    Server.VoteIsOn = true;
                    Logger.Log(LogType.SystemActivity, "{0} started a votekick on player {1} reason: {2}", player.Name, target.Name, reason);
                    Scheduler.NewTask(t => VoteKickCheck(player)).RunOnce(TimeSpan.FromSeconds(60));
                    Server.VoteKickReason = reason;
                    Server.TargetName = target.Name;
                    break;

                case "no":
                    if (!Server.VoteIsOn)
                    {
                        player.Message("No vote is currently running");
                        return;
                    }
                    if (player.Info.HasVoted)
                    {
                        player.Message("&CYou have already voted");
                        return;
                    }
                    Server.VoteNo++;
                    Server.Voted.Add(player);
                    player.Info.HasVoted = true;
                    player.Message("&8You have voted for 'No'");
                    break;

                case "ask":
                    string question = cmd.NextAll();
                    if (!player.Can(Permission.MakeVotes))
                    {
                        player.Message("You do not have permissions to ask a question");
                        return;
                    }
                    if (Server.VoteIsOn)
                    {
                        player.Message("A vote has already started. Each vote lasts 1 minute.");
                        return;
                    }
                    if (question.Length < 5)
                    {
                        player.Message("Invalid question");
                        return;
                    }
                    Server.Players.Message("{0}&S Asked: {1}", player.ClassyName, question);
                    Server.Players.Message("&9Vote now! &S/Vote &AYes &Sor /Vote &CNo");
                    Server.VoteIsOn = true;
                    Scheduler.NewTask(t => VoteCheck(player)).RunOnce(TimeSpan.FromSeconds(60));
                    Server.Question = question;
                    Server.VoteAskStarter = player.ClassyName;
                    break;
            }
        }

        static void VoteCheck(Player player)
        {
            if (Server.VoteIsOn)
            {
                Server.Players.Message("{0}&S Asked: {1} \n&SResults are in! Yes: &A{2} &SNo: &C{3}", player.ClassyName,
                                       Server.Question, Server.VoteYes, Server.VoteNo);
                Server.VoteYes = 0;
                Server.VoteNo = 0;
                Server.VoteIsOn = false;
                Server.VoteAskStarter = null;

                foreach (Player V in Server.Voted)
                {
                    V.Info.HasVoted = false;
                }
            }
        }

        static void VoteKickCheck(Player player)
        {
            if (Server.VoteIsOn)
            {
                Server.Players.Message("{0}&S wanted to get {1} kicked. Reason: {2} \n&SResults are in! Yes: &A{3} &SNo: &C{4}", player.ClassyName,
                                       Server.TargetName, Server.VoteKickReason, Server.VoteYes, Server.VoteNo);

                Player target = Server.FindPlayerOrPrintMatches(Player.Console, Server.TargetName, true, true);

                if (Server.VoteYes > Server.VoteNo){
                    Scheduler.NewTask(t => target.Kick(Player.Console, "VoteKick by: " + player.Name + " - " + Server.VoteKickReason, LeaveReason.Kick, false, true, false)).RunOnce(TimeSpan.FromSeconds(3));
                    Server.Players.Message("{0}&S was kicked from the server", target.ClassyName);
                }
                else
                    Server.Players.Message("{0} &Sdid not get kicked from the server", target.ClassyName);

                Server.VoteYes = 0;
                Server.VoteNo = 0;
                Server.VoteIsOn = false;
                Server.VoteKickReason = null;

                foreach (Player V in Server.Voted)
                {
                    V.Info.HasVoted = false;
                }
            }
        }

        static readonly CommandDescriptor CdCustomChat = new CommandDescriptor
        {
            Name = ConfigKey.CustomChatChannel.GetString(),
            Category = CommandCategory.Chat,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = true,
            NotRepeatable = true,
            Usage = "/Customname Message",
            Help = "Broadcasts your message to all players allowed to read the CustomChatChannel.",
            Handler = EngineerHandler
        };

        static void EngineerHandler(Player player, Command cmd)
        {
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }

            if (player.DetectChatSpam()) return;

            string message = cmd.NextAll().Trim();
            if (message.Length > 0)
            {
                if (player.Can(Permission.UseColorCodes) && message.Contains("%"))
                {
                    message = Color.ReplacePercentCodes(message);
                }
                Chat.SendCustom(player, message);
            }
        }

        #region Troll

        static readonly CommandDescriptor CdTroll = new CommandDescriptor
        {
            Name = "Troll",
            Category = CommandCategory.Chat,
            Permissions = new[] { Permission.Troll },
            IsConsoleSafe = true,
            NotRepeatable = false,
            Usage = "/troll option player Message",
            Help = "Does a little somthin'-somethin'.",
            Handler = TrollHandler
        };

        static void TrollHandler(Player player, Command cmd)
        {
            string options = cmd.Next();
            switch (options)
            {
                case "pm":

                    string pName = cmd.Next();
                    string msg = cmd.NextAll().Trim();

                    if (pName == null)
                    {
                        player.Message("Player not found. Please specify valid name.");
                        return;
                    }

                    Player target = Server.FindPlayerOrPrintMatches(player, pName, true, true);

                    if (target == null)
                        return;

                    if (!Player.IsValidName(pName))
                    {
                        player.Message("Player not found. Please specify valid name.");
                        return;
                    }

                    if (msg.Length < 1)
                    {
                        player.Message("Error: Please enter a message for {0}.", target.ClassyName);
                        return;
                    }

                    else
                    {
                        if (player.Can(Permission.UseColorCodes) && msg.Contains("%"))
                        {
                            msg = Color.ReplacePercentCodes(msg);
                        }
                        Server.Players.Message("&Pfrom {0}: {1}", target.Name, msg);
                    }
                    break;
                case "ac":
                    string aName = cmd.Next();
                    string msgAc = cmd.NextAll().Trim();

                    Player target2 = Server.FindPlayerOrPrintMatches(player, aName, true, true);
                    if (target2 == null)
                    {
                        player.Message("Please enter a valid name.");
                        return;
                    }
                    if (Player.IsInValidName(aName) || aName == null)
                    {
                        player.Message("Player not found. Please specify valid name.");
                        return;
                    }

                    if (msgAc.Length < 1)
                    {
                        player.Message("Error: Please enter a message for {0}.", target2.ClassyName);
                        return;
                    }
                    else
                    {
                        Chat.SendAdmin(target2, msgAc);
                    }
                    break;
                
                case "st":
                case "staff":
                    string SName = cmd.Next();
                    string msgSc = cmd.NextAll().Trim();

                    if (Player.IsInValidName(SName) || msgSc == null)
                    {
                        player.Message("Player not found. Please specify valid name.");
                        return;
                    }
                    Player target4 = Server.FindPlayerOrPrintMatches(player, SName, true, true);

                    if (target4 == null)
                    {
                        player.Message("Please enter a valid name.");
                        return;
                    }
                    if (msgSc.Length < 1)
                    {
                        player.Message("Error: Please enter a message for {0}.", target4.ClassyName);
                        return;
                    }
                    else
                    {
                        Chat.SendStaff(target4, msgSc);
                    }
                    break;
                case "i":
                case "impersonate":
                case "msg":
                case "message":
                case "m":
                    string name = cmd.Next();
                    if (player.Info.IsMuted)
                    {
                        player.MessageMuted();
                        return;
                    }

                    if (Player.IsInValidName(name) || name == null)
                    {
                        player.Message("Player not found. Please specify valid name.");
                        return;
                    }

                    if (player.Can(Permission.Chat))
                    {
                        string msg2 = cmd.NextAll().Trim();

                        Player target5 = Server.FindPlayerOrPrintMatches(player, name, true, true);
                        if (target5 == null)
                        {
                            player.Message("Please enter a valid name.");
                            return;
                        }

                        if (msg2.Length > 0)
                        {
                            Server.Message("{0}&S&F: {1}",
                                              target5.ClassyName, msg2);
                            return;
                        }

                        else
                        {
                            player.Message("&SYou need to enter a message");
                            return;
                        }

                    } break;
                case "leave":
                case "disconnect":
                case "gtfo":
                    string elol = cmd.Next();
                    if (elol == null)
                    { player.Message("Nope"); return; }

                    if (Player.IsInValidName(elol))
                    {
                        player.Message("Player not found. Please specify valid name.");
                        return;
                    }
                    else
                    {
                        Player target6 = Server.FindPlayerOrPrintMatches(player, elol, true, true);
                        if (target6 == null)
                        {
                            player.Message("Please enter a valid name.");
                            return;
                        }
                        Server.Players.Message("&SPlayer {0}&S left the server.", target6.ClassyName);

                    }
                    break;
                default: player.Message("Invalid option. Please choose st, ac, pm, message or leave");
                    break;
            }
        }

        #endregion

        static readonly CommandDescriptor cdAway = new CommandDescriptor
        {
            Name = "Away",
            Category = CommandCategory.Chat,
            Aliases = new[] { "afk" },
            IsConsoleSafe = true,
            Usage = "/away [optional message]",
            Help = "Shows an away message.",
            NotRepeatable = true,
            Handler = Away
        };

        internal static void Away(Player player, Command cmd)
        {
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }

            if (player.Can(Permission.Chat))
            {
                string msg = cmd.NextAll().Trim();

                if (msg.Length > 0)
                {
                    Server.Message("{0}&S &Eis away &9({1})",
                                      player.ClassyName, msg);
                    player.IsAway = true;
                    return;
                }

                else
                {
                    player.IsAway = true;
                    Server.Players.Message("&S{0} &Eis away &9(Away From Keyboard)", player.ClassyName);
                }
            }
        }

        static readonly CommandDescriptor cdHigh5 = new CommandDescriptor
        {
            Name = "High5",
            Aliases = new string[] { "h5" },
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Usage = "/high5 playername",
            Help = "High fives a given player.",
            NotRepeatable = true,
            Handler = High5,
            Permissions = new Permission[] { Permission.HighFive }
        };

        internal static void High5(Player player, Command cmd)
        {
            string targetName = cmd.Next();

            if (targetName == null)
            {
                cdHigh5.PrintUsage(player);
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, targetName, false, true);

            if (target == null)
            {
                player.MessageNoPlayer(targetName);
                return;
            }
            if (target == player)
            {
                player.Message("You cannot high five yourself.");
                return;
            }
            else
            {
                Server.Players.CanSee(target).Except(player).Message("{0}&S was just &chigh fived &Sby {1}&S", target.ClassyName, player.ClassyName);
                target.Message("{0}&S high fived you.", player.ClassyName);

                player.Message("Player {0}&S is not online.", target.ClassyName);
            }
        }

        static readonly CommandDescriptor CdPoke = new CommandDescriptor
        {
            Name = "Poke",
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Usage = "/poke playername",
            Help = "Pokes a Player.",
            NotRepeatable = true,
            Handler = PokeHandler
        };

        internal static void PokeHandler(Player player, Command cmd)
        {
            string targetName = cmd.Next();
            if (targetName == null)
            {
                CdPoke.PrintUsage(player);
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches(player, targetName, false, true);

            if (target == null)
            {
                player.Message("Please enter the name of the player you want to poke.");
                return;
            }

            if (target == player)
            {
                player.Message("You cannot poke yourself.");
                return;
            }

            if (Player.IsInValidName(targetName))
            {
                player.Message("Player not found. Please specify valid name.");
                return;
            }

            else
            {
                target.Message("&8You were just poked by {0}",
                                  player.ClassyName);
                player.Message("&8Successfully poked {0}", target.ClassyName);
            }
        }

        static readonly CommandDescriptor cdReview = new CommandDescriptor
        {
            Name = "Review",
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Usage = "/review",
            NotRepeatable = true,
            Help = "Request an Op to review your build.",
            Handler = Review
        };

        internal static void Review(Player player, Command cmd)
        {
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }

            var recepientList = Server.Players.Can(Permission.ReadStaffChat)
                                              .NotIgnoring(player)
                                              .Union(player);
            string message = String.Format("{0}&6 would like staff to check their build", player.ClassyName);
            recepientList.Message(message);
            var RevieweeNames = Server.Players
                                         .CanBeSeen(player)
                                         .Where(r => r.Can(Permission.Promote, player.Info.Rank));
            if (RevieweeNames.Count() > 0)
            {
                player.Message("&WOnline players who can review you: {0}", RevieweeNames.JoinToString(r => String.Format("{0}&S", r.ClassyName)));
                return;
            }
            else
                player.Message("&WThere are no players online who can review you. A member of staff needs to be online.");
        }

        static readonly CommandDescriptor CdAdminChat = new CommandDescriptor
        {
            Name = "Adminchat",
            Aliases = new[] { "ac" },
            Category = CommandCategory.Chat | CommandCategory.Moderation,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = true,
            NotRepeatable = true,
            Usage = "/Adminchat Message",
            Help = "Broadcasts your message to admins/owners on the server.",
            Handler = AdminChat
        };

        internal static void AdminChat(Player player, Command cmd)
        {
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }

            if (DateTime.UtcNow < player.Info.MutedUntil)
            {
                player.Message("You are muted for another {0:0} seconds.",
                                player.Info.MutedUntil.Subtract(DateTime.UtcNow).TotalSeconds);
                return;
            }

            string message = cmd.NextAll().Trim();
            if (message.Length > 0)
            {
                if (player.Can(Permission.UseColorCodes) && message.Contains("%"))
                {
                    message = Color.ReplacePercentCodes(message);
                }
                Chat.SendAdmin(player, message);
            }
        }

        #region Say

        static readonly CommandDescriptor CdSay = new CommandDescriptor
        {
            Name = "Say",
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            NotRepeatable = true,
            DisableLogging = true,
            Permissions = new[] { Permission.Chat, Permission.Say },
            Usage = "/Say Message",
            Help = "Shows a message in special color, without the player name prefix. " +
                   "Can be used for making announcements.",
            Handler = SayHandler
        };

        static void SayHandler(Player player, Command cmd)
        {
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }

            if (player.DetectChatSpam()) return;

            if (player.Can(Permission.Say))
            {
                string msg = cmd.NextAll().Trim();
                if (player.Can(Permission.UseColorCodes) && msg.Contains("%"))
                {
                    msg = Color.ReplacePercentCodes(msg);
                }
                if (msg.Length > 0)
                {
                    Chat.SendSay(player, msg);
                }
                else
                {
                    CdSay.PrintUsage(player);
                }
            }
            else
            {
                player.MessageNoAccess(Permission.Say);
            }
        }

        #endregion


        #region Staff

        static readonly CommandDescriptor CdStaff = new CommandDescriptor
        {
            Name = "Staff",
            Aliases = new[] { "st" },
            Category = CommandCategory.Chat | CommandCategory.Moderation,
            Permissions = new[] { Permission.Chat },
            NotRepeatable = true,
            IsConsoleSafe = true,
            DisableLogging = true,
            Usage = "/Staff Message",
            Help = "Broadcasts your message to all operators/moderators on the server at once.",
            Handler = StaffHandler
        };

        static void StaffHandler(Player player, Command cmd)
        {
            string message = cmd.NextAll().Trim();

            if(message == "static")
            {
                if (player.IsStaticStaff)
                {
                    player.IsStaticStaff = false;
                    player.Message("&W(Staff): Static mode is now OFF.");
                    return;
                }

                if (!player.IsStaticStaff)
                {
                    player.IsStaticStaff = true;
                    player.Message("&W(Staff): Static mode is now ON. Use /Staff to turn OFF");
                    return;
                }
            }
            
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }

            if (player.DetectChatSpam()) return;

            if (message.Length > 0)
            {
                if (player.Can(Permission.UseColorCodes) && message.Contains("%"))
                {
                    message = Color.ReplacePercentCodes(message);
                }
                Chat.SendStaff(player, message);
            }
        }

        #endregion


        #region Ignore / Unignore

        static readonly CommandDescriptor CdIgnore = new CommandDescriptor
        {
            Name = "Ignore",
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Usage = "/Ignore [PlayerName]",
            Help = "Temporarily blocks the other player from messaging you. " +
                   "If no player name is given, lists all ignored players.",
            Handler = IgnoreHandler
        };

        static void IgnoreHandler(Player player, Command cmd)
        {
            string name = cmd.Next();
            if (name != null)
            {
                if (cmd.HasNext)
                {
                    CdIgnore.PrintUsage(player);
                    return;
                }
                PlayerInfo targetInfo = PlayerDB.FindPlayerInfoOrPrintMatches(player, name);
                if (targetInfo == null) return;

                if (player.Ignore(targetInfo))
                {
                    player.MessageNow("You are now ignoring {0}", targetInfo.ClassyName);
                }
                else
                {
                    player.MessageNow("You are already ignoring {0}", targetInfo.ClassyName);
                }
            }
            else
            {
                PlayerInfo[] ignoreList = player.IgnoreList;
                if (ignoreList.Length > 0)
                {
                    player.MessageNow("Ignored players: {0}", ignoreList.JoinToClassyString());
                }
                else
                {
                    player.MessageNow("You are not currently ignoring anyone.");
                }
                return;
            }
        }


        static readonly CommandDescriptor CdUnignore = new CommandDescriptor
        {
            Name = "Unignore",
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Usage = "/Unignore PlayerName",
            Help = "Unblocks the other player from messaging you.",
            Handler = UnignoreHandler
        };

        static void UnignoreHandler(Player player, Command cmd)
        {
            string name = cmd.Next();
            if (name != null)
            {
                if (cmd.HasNext)
                {
                    CdUnignore.PrintUsage(player);
                    return;
                }
                PlayerInfo targetInfo = PlayerDB.FindPlayerInfoOrPrintMatches(player, name);
                if (targetInfo == null) return;

                if (player.Unignore(targetInfo))
                {
                    player.MessageNow("You are no longer ignoring {0}", targetInfo.ClassyName);
                }
                else
                {
                    player.MessageNow("You are not currently ignoring {0}", targetInfo.ClassyName);
                }
            }
            else
            {
                PlayerInfo[] ignoreList = player.IgnoreList;
                if (ignoreList.Length > 0)
                {
                    player.MessageNow("Ignored players: {0}", ignoreList.JoinToClassyString());
                }
                else
                {
                    player.MessageNow("You are not currently ignoring anyone.");
                }
                return;
            }
        }

        #endregion


        #region Me

        static readonly CommandDescriptor CdMe = new CommandDescriptor
        {
            Name = "Me",
            Category = CommandCategory.Chat,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = true,
            NotRepeatable = true,
            DisableLogging = true,
            Usage = "/Me Message",
            Help = "Sends IRC-style action message prefixed with your name.",
            Handler = MeHandler
        };

        static void MeHandler(Player player, Command cmd)
        {
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }

            if (player.DetectChatSpam()) return;

            string msg = cmd.NextAll().Trim();
            if (msg.Length > 0)
            {
                player.Info.ProcessMessageWritten();
                if (player.Can(Permission.UseColorCodes) && msg.Contains("%"))
                {
                    msg = Color.ReplacePercentCodes(msg);
                }
                Chat.SendMe(player, msg);
            }
        }

        #endregion


        #region Roll

        static readonly CommandDescriptor CdRoll = new CommandDescriptor
        {
            Name = "Roll",
            Category = CommandCategory.Chat,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = true,
            Help = "Gives random number between 1 and 100.\n" +
                   "&H/Roll MaxNumber\n" +
                   "&S  Gives number between 1 and max.\n" +
                   "&H/Roll MinNumber MaxNumber\n" +
                   "&S  Gives number between min and max.",
            Handler = RollHandler
        };

        static void RollHandler(Player player, Command cmd)
        {
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }

            if (player.DetectChatSpam()) return;

            Random rand = new Random();
            int n1;
            int min, max;
            if (cmd.NextInt(out n1))
            {
                int n2;
                if (!cmd.NextInt(out n2))
                {
                    n2 = 1;
                }
                min = Math.Min(n1, n2);
                max = Math.Max(n1, n2);
            }
            else
            {
                min = 1;
                max = 100;
            }

            int num = rand.Next(min, max + 1);
            Server.Message(player,
                            "{0}{1} rolled {2} ({3}...{4})",
                            player.ClassyName, Color.Silver, num, min, max);
            player.Message("{0}You rolled {1} ({2}...{3})",
                            Color.Silver, num, min, max);
        }

        #endregion


        #region Deafen

        static readonly CommandDescriptor CdDeafen = new CommandDescriptor
        {
            Name = "Deafen",
            Aliases = new[] { "deaf" },
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Help = "Blocks all chat messages from being sent to you.",
            Handler = DeafenHandler
        };

        static void DeafenHandler(Player player, Command cmd)
        {
            if (cmd.HasNext)
            {
                CdDeafen.PrintUsage(player);
                return;
            }
            if (!player.IsDeaf)
            {
                for (int i = 0; i < LinesToClear; i++)
                {
                    player.MessageNow("");
                }
                player.MessageNow("Deafened mode: ON");
                player.MessageNow("You will not see ANY messages until you type &H/Deafen&S again.");
                player.IsDeaf = true;
            }
            else
            {
                player.IsDeaf = false;
                player.MessageNow("Deafened mode: OFF");
            }
        }

        #endregion


        #region Clear

        const int LinesToClear = 30;
        static readonly CommandDescriptor CdClear = new CommandDescriptor
        {
            Name = "Clear",
            UsableByFrozenPlayers = true,
            Category = CommandCategory.Chat,
            Help = "Clears the chat screen.",
            Handler = ClearHandler
        };

        static void ClearHandler(Player player, Command cmd)
        {
            if (cmd.HasNext)
            {
                CdClear.PrintUsage(player);
                return;
            }
            for (int i = 0; i < LinesToClear; i++)
            {
                player.Message("");
            }
        }

        #endregion


        #region Timer

        static readonly CommandDescriptor CdTimer = new CommandDescriptor
        {
            Name = "Timer",
            Permissions = new[] { Permission.Say },
            IsConsoleSafe = true,
            Category = CommandCategory.Chat,
            Usage = "/Timer <Duration> <Message>",
            Help = "Starts a timer with a given duration and message. " +
                   "As the timer counts down, announcements are shown globally. See also: &H/Help Timer Abort",
            HelpSections = new Dictionary<string, string> {
                { "abort",  "&H/Timer Abort <TimerID>\n&S" +
                            "Aborts a timer with the given ID number. " +
                            "To see a list of timers and their IDs, type &H/Timer&S (without any parameters)." }
            },
            Handler = TimerHandler
        };

        static void TimerHandler(Player player, Command cmd)
        {
            string param = cmd.Next();

            // List timers
            if (param == null)
            {
                ChatTimer[] list = ChatTimer.TimerList.OrderBy(timer => timer.TimeLeft).ToArray();
                if (list.Length == 0)
                {
                    player.Message("No timers running.");
                }
                else
                {
                    player.Message("There are {0} timers running:", list.Length);
                    foreach (ChatTimer timer in list)
                    {
                        player.Message("  #{0} \"{1}\" (started by {2}, {3} left)",
                                        timer.Id, timer.Message, timer.StartedBy, timer.TimeLeft.ToMiniString());
                    }
                }
                return;
            }

            // Abort a timer
            if (param.Equals("abort", StringComparison.OrdinalIgnoreCase))
            {
                int timerId;
                if (cmd.NextInt(out timerId))
                {
                    ChatTimer timer = ChatTimer.FindTimerById(timerId);
                    if (timer == null || !timer.IsRunning)
                    {
                        player.Message("Given timer (#{0}) does not exist.", timerId);
                    }
                    else
                    {
                        timer.Stop();
                        string abortMsg = String.Format("&Y(Timer) {0}&Y aborted a timer with {1} left: {2}",
                                                         player.ClassyName, timer.TimeLeft.ToMiniString(), timer.Message);
                        Chat.SendSay(player, abortMsg);
                    }
                }
                else
                {
                    CdTimer.PrintUsage(player);
                }
                return;
            }

            // Start a timer
            if (player.Info.IsMuted)
            {
                player.MessageMuted();
                return;
            }
            if (player.DetectChatSpam()) return;
            TimeSpan duration;
            if (!param.TryParseMiniTimespan(out duration))
            {
                CdTimer.PrintUsage(player);
                return;
            }
            if (duration < ChatTimer.MinDuration)
            {
                player.Message("Timer: Must be at least 1 second.");
                return;
            }

            string sayMessage;
            string message = cmd.NextAll();
            if (String.IsNullOrEmpty(message))
            {
                sayMessage = String.Format("&Y(Timer) {0}&Y started a {1} timer",
                                            player.ClassyName,
                                            duration.ToMiniString());
            }
            else
            {
                sayMessage = String.Format("&Y(Timer) {0}&Y started a {1} timer: {2}",
                                            player.ClassyName,
                                            duration.ToMiniString(),
                                            message);
            }
            Chat.SendSay(player, sayMessage);
            ChatTimer.Start(duration, message, player.Name);
        }

        #endregion
    }
}