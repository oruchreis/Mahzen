namespace Mahzen.Core
{
    /// <summary>
    /// Using for creating map protocol object items
    /// </summary>
    public class MapItemProtocolBuilder : ProtocolBuilder
    {
        /// <summary>
        /// Created map item
        /// </summary>
        public MessageProtocolObject ProtocolObject { get; set; }

        /// <inheritdoc />
        protected override void HandleWrite(MessageProtocolObject protocolObject)
        {
            ProtocolObject = protocolObject;
        }
    }
}
