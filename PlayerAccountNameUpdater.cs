using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;

namespace PlayerAccountNameUpdater
{
    [ApiVersion(2, 1)]
    public class PlayerAccountNameUpdater : TerrariaPlugin
    {
        public override string Name => "Player Account Name Updater";
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "jgranserver";
        public override string Description =>
            "Updates player account names by its new player name if they differ.";

        private const int REMINDER_INTERVAL = 600; // 10 minutes in seconds
        private Dictionary<string, DateTime> _lastReminderTime = new Dictionary<string, DateTime>();
        private Dictionary<string, bool> _pendingNameChanges = new Dictionary<string, bool>();

        public PlayerAccountNameUpdater(Main game)
            : base(game) { }

        public override void Initialize()
        {
            PlayerHooks.PlayerPostLogin += OnPlayerPostLogin;
            Commands.ChatCommands.Add(
                new Command("updateaccountname.confirm", ConfirmNameChange, "confirmname")
            );
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        }

        private void OnPlayerPostLogin(TShockAPI.Hooks.PlayerPostLoginEventArgs args)
        {
            var player = args.Player;
            if (player == null || !player.IsLoggedIn)
                return;

            var account = player.Account;
            if (account == null)
                return;

            if (account.Name != player.Name)
            {
                _pendingNameChanges[player.Name] = false;
                player.SendInfoMessage(
                    $"Server detected that your account name ({account.Name}) differs from your player name ({player.Name})."
                );
                player.SendInfoMessage("To update your account name, use: /confirmname <password>");
            }
        }

        private bool VerifyPassword(string accountName, string password)
        {
            try
            {
                var account = TShock.UserAccounts.GetUserAccountByName(accountName);
                if (account == null)
                    return false;

                return BCrypt.Net.BCrypt.Verify(password, account.Password);
            }
            catch
            {
                return false;
            }
        }

        private void ConfirmNameChange(CommandArgs args)
        {
            var player = args.Player;
            if (player == null || !player.IsLoggedIn)
            {
                args.Player.SendErrorMessage("You must be logged in to use this command.");
                return;
            }

            if (!_pendingNameChanges.ContainsKey(player.Name))
            {
                player.SendErrorMessage("You don't have any pending name changes.");
                return;
            }

            if (args.Parameters.Count < 1)
            {
                player.SendErrorMessage("Usage: /confirmname <password>");
                return;
            }

            string password = args.Parameters[0];
            var account = player.Account;

            if (!VerifyPassword(account.Name, password))
            {
                player.SendErrorMessage("Invalid password!");
                return;
            }

            string oldName = account.Name;
            string error;
            if (!UpdateUserAccount(account, player.Name, out error))
            {
                player.SendErrorMessage(error);
                return;
            }

            _pendingNameChanges.Remove(player.Name);
            _lastReminderTime.Remove(player.Name);
            player.SendSuccessMessage(
                $"Successfully updated account name from '{oldName}' to '{player.Name}'!"
            );
        }

        private bool UpdateUserAccount(UserAccount account, string newName, out string error)
        {
            error = string.Empty;
            try
            {
                // Check if the new name is already taken
                var existingAccount = TShock.UserAccounts.GetUserAccountByName(newName);
                if (existingAccount != null && existingAccount.ID != account.ID)
                {
                    error = "An account with that name already exists!";
                    return false;
                }

                // Update account in database
                using (var db = TShock.DB)
                {
                    db.Query("UPDATE Users SET Username=@0 WHERE ID=@1", newName, account.ID);
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = $"Failed to update account: {ex.Message}";
                TShock.Log.Error($"Account update error: {ex}");
                return false;
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            foreach (var kvp in _pendingNameChanges.ToList())
            {
                string playerName = kvp.Key;
                TSPlayer player = TShock.Players.FirstOrDefault(p => p?.Name == playerName);

                if (player == null || !player.IsLoggedIn)
                    continue;

                if (!_lastReminderTime.ContainsKey(playerName))
                {
                    _lastReminderTime[playerName] = DateTime.Now;
                    continue;
                }

                if (
                    (DateTime.Now - _lastReminderTime[playerName]).TotalSeconds >= REMINDER_INTERVAL
                )
                {
                    player.SendInfoMessage(
                        "Reminder: Your account name differs from your player name."
                    );
                    player.SendInfoMessage(
                        "Use: /confirmname <password> to update your account name."
                    );
                    _lastReminderTime[playerName] = DateTime.Now;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                PlayerHooks.PlayerPostLogin -= OnPlayerPostLogin;
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            }
            base.Dispose(disposing);
        }
    }
}
