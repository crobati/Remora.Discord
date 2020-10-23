//
//  NamedParameterShape.cs
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
using System.Collections.Generic;
using Remora.Commands.Tokenization;

namespace Remora.Commands.Signatures
{
    /// <summary>
    /// Represents a named parameter with a single value.
    /// </summary>
    public class NamedParameterShape : IParameterShape
    {
        /// <summary>
        /// Gets the short name of the parameter, if any. At least one of <see cref="ShortName"/> and
        /// <see cref="LongName"/> must be set.
        /// </summary>
        public char? ShortName { get; }

        /// <summary>
        /// Gets the long name of the parameter, if any. At least one of <see cref="ShortName"/> and
        /// <see cref="LongName"/> must be set.
        /// </summary>
        public string? LongName { get; }

        /// <inheritdoc/>
        public virtual IReadOnlyList<TokenType> ValidTypes =>
            new[] { TokenType.LongName, TokenType.ShortName, TokenType.Value };

        /// <inheritdoc />
        public bool IsRequired { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedParameterShape"/> class.
        /// </summary>
        /// <param name="shortName">The short name.</param>
        /// <param name="longName">The long name.</param>
        /// <param name="isRequired">Whether the parameter is required.</param>
        public NamedParameterShape(char shortName, string longName, bool isRequired)
        {
            this.ShortName = shortName;
            this.LongName = longName;
            this.IsRequired = isRequired;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedParameterShape"/> class.
        /// </summary>
        /// <param name="longName">The long name.</param>
        /// <param name="isRequired">Whether the parameter is required.</param>
        /// <exception cref="ArgumentException">Thrown if neither the short nor the long name is set.</exception>
        public NamedParameterShape(string longName, bool isRequired)
        {
            this.LongName = longName;
            this.IsRequired = isRequired;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedParameterShape"/> class.
        /// </summary>
        /// <param name="shortName">The short name.</param>
        /// <param name="isRequired">Whether the parameter is required.</param>
        /// <exception cref="ArgumentException">Thrown if neither the short nor the long name is set.</exception>
        public NamedParameterShape(char shortName, bool isRequired)
        {
            this.ShortName = shortName;
            this.IsRequired = isRequired;
        }

        /// <inheritdoc/>
        public virtual bool Matches(TokenizingEnumerator tokenizer, out ulong consumedTokens)
        {
            consumedTokens = 0;

            if (!tokenizer.MoveNext())
            {
                return false;
            }

            switch (tokenizer.Current.Type)
            {
                case TokenType.LongName:
                {
                    if (this.LongName is null)
                    {
                        return false;
                    }

                    if (!tokenizer.Current.Value.Equals(this.LongName, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    break;
                }
                case TokenType.ShortName:
                {
                    if (this.ShortName is null)
                    {
                        return false;
                    }

                    if (tokenizer.Current.Value.Length != 1)
                    {
                        return false;
                    }

                    if (tokenizer.Current.Value[0] != this.ShortName.Value)
                    {
                        return false;
                    }

                    break;
                }
                case TokenType.Value:
                {
                    return false;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            if (!tokenizer.MoveNext())
            {
                return false;
            }

            if (tokenizer.Current.Type != TokenType.Value)
            {
                return false;
            }

            consumedTokens = 2;
            return true;
        }
    }
}