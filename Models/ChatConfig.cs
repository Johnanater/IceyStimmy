extern alias JetBrainsAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace IceyStimmy.Models
{
    [JetBrainsAnnotations::JetBrains.Annotations.UsedImplicitlyAttribute]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class ChatConfig
    {
        public string ChatMessageIconUrl { get; set; }
        public bool ChatUseRichText { get; set; }
        public string ChatMessageColor { get; set; }
        public string ErrorChatMessageColor { get; set; }
    }
}
