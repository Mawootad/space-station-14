﻿#nullable enable
using System;
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Actions
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class CooldownAction : IClientCommand
    {
        public string Command => "coolaction";
        public string Description => "Sets a cooldown on an action for a player, defaulting to current player";
        public string Help => "coolaction <actionType> <seconds> <name or userID, omit for current player>";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player == null) return;
            var attachedEntity = player.AttachedEntity;
            if (args.Length > 2)
            {
                var target = args[2];
                if (!CommandUtils.TryGetAttachedEntityByUsernameOrId(shell, target, player, out attachedEntity)) return;
            }

            if (attachedEntity == null) return;
            if (!attachedEntity.TryGetComponent(out ServerActionsComponent? actionsComponent))
            {
                shell.SendText(player, "user has no actions component");
                return;
            }

            var actionTypeRaw = args[0];
            if (!Enum.TryParse<ActionType>(actionTypeRaw, out var actionType))
            {
                shell.SendText(player, "unrecognized ActionType enum value, please" +
                                       " ensure you used correct casing: " + actionTypeRaw);
                return;
            }
            var actionMgr = IoCManager.Resolve<ActionManager>();

            if (!actionMgr.TryGet(actionType, out var action))
            {
                shell.SendText(player, "unrecognized actionType " + actionType);
                return;
            }

            var cooldownStart = IoCManager.Resolve<IGameTiming>().CurTime;
            if (!uint.TryParse(args[1], out var seconds))
            {
                shell.SendText(player, "cannot parse seconds: " + args[1]);
                return;
            }

            var cooldownEnd = cooldownStart.Add(TimeSpan.FromSeconds(seconds));

            actionsComponent.Cooldown(action.ActionType, (cooldownStart, cooldownEnd));
        }
    }
}
