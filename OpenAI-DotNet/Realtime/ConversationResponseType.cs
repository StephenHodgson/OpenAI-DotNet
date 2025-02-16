// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.Serialization;

namespace OpenAI.Realtime
{
    public enum ConversationResponseType
    {
        [EnumMember(Value = "auto")]
        Auto = 0,
        [EnumMember(Value = "none")]
        None = 1
    }
}
