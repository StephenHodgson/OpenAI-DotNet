// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.Serialization;

namespace OpenAI.Realtime
{
    public enum ConversationItemType
    {
        [EnumMember(Value = "message")]
        Message,
        [EnumMember(Value = "function_call")]
        FunctionCall,
        [EnumMember(Value = "function_call_output")]
        FunctionCallOutput,
        [EnumMember(Value = "item_reference")]
        ItemReference
    }
}
