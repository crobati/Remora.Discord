//
//  DiceRollCommands.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Json;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Discord.Samples.DiceRoller.API;
using Remora.Results;

namespace Remora.Discord.Samples.DiceRoller.Commands
{
    /// <summary>
    /// Contains commands for rolling dice.
    /// </summary>
    public class DiceRollCommands : CommandGroup, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly ICommandContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiceRollCommands"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The http client factory used for API requests.</param>
        /// <param name="channelAPI">The Discord channel API.</param>
        /// <param name="context">Additional context about the original message.</param>
        public DiceRollCommands
        (
            IHttpClientFactory httpClientFactory,
            IDiscordRestChannelAPI channelAPI,
            ICommandContext context
        )
        {
            _httpClient = httpClientFactory.CreateClient();
            _channelAPI = channelAPI;
            _context = context;
        }

        /// <summary>
        /// Rolls a dice using an online service.
        /// </summary>
        /// <param name="value">The command to send to the online service.</param>
        /// <returns>The result of the operation.</returns>
        [Command("roll")]
        public async Task<Result> RollDiceAsync(string value)
        {
            var rollRequests = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (rollRequests.Length == 0)
            {
                return Result.FromSuccess();
            }

            var getRolls = await GetRollsAsync(rollRequests);
            if (!getRolls.IsSuccess)
            {
                var replyWithFailure = await ReplyWithFailureAsync(_context.ChannelID);

                return replyWithFailure.IsSuccess
                    ? Result.FromError(getRolls)
                    : replyWithFailure;
            }

            var rollResponse = getRolls.Entity;

            return await ReplyWithRollsAsync(_context.ChannelID, rollResponse);
        }

        private async Task<Result<RollResponse>> GetRollsAsync(string[] parsedRollRequests)
        {
            var requestUrl = $"http://roll.diceapi.com/json/{string.Join('/', parsedRollRequests)}";

            using var response = await _httpClient.GetAsync(requestUrl);
            if (!response.IsSuccessStatusCode)
            {
                return new GenericError(response.ReasonPhrase ?? "No reason given.");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync();

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = new SnakeCaseNamingPolicy() };
            var rollResponse = await JsonSerializer.DeserializeAsync<RollResponse>(responseStream, jsonOptions);

            if (rollResponse is null)
            {
                return new GenericError("The roll response was null.");
            }

            return !rollResponse.Success
                ? new GenericError("Dice rolling failed :(")
                : rollResponse;
        }

        private async Task<Result> ReplyWithFailureAsync(Snowflake channel)
        {
            var failEmbed = new Embed(Description: "Dice rolling failed :(", Colour: Color.OrangeRed);

            var replyFail = await _channelAPI.CreateMessageAsync(channel, embed: failEmbed);

            return !replyFail.IsSuccess
                ? Result.FromError(replyFail)
                : Result.FromSuccess();
        }

        private async Task<Result> ReplyWithRollsAsync(Snowflake channel, RollResponse rollResponse)
        {
            var rolls = rollResponse.Dice
                .GroupBy(d => d.Type)
                .ToDictionary
                (
                    g => g.Key,
                    g => g.Select(d => d.Value).Aggregate((a, b) => a + b)
                );

            var fields = rolls.Select(kvp => new EmbedField(kvp.Key, kvp.Value.ToString(), true)).ToList();
            var embed = new Embed("Rolls", Fields: fields, Colour: Color.LawnGreen);

            var replyRolls = await _channelAPI.CreateMessageAsync(channel, embed: embed);

            return !replyRolls.IsSuccess
                ? Result.FromError(replyRolls)
                : Result.FromSuccess();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
