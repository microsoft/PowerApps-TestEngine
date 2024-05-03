namespace Microsoft.PowerApps.TestEngine.Modules
{
    /// <summary>
    /// A wrapper object to store information about a tested certificate source
    /// </summary> <summary>
    /// 
    /// </summary>
    public class TestEngineTrustSource
    {
        /// <summary>
        /// The name of the certificate
        /// </summary>
        /// <value></value>
        public string Name { get; set; }

        /// <summary>
        /// The organization who has issued the certificate
        /// </summary>
        /// <value></value>
        public string Organization { get; set; }

        /// <summary>
        /// The location of the organization
        /// </summary>
        /// <value></value>
        public string Location { get; set; }

        /// <summary>
        /// The state that the organization is within
        /// </summary>
        /// <value></value>
        public string State { get; set; }

        /// <summary>
        /// The country code
        /// </summary>
        /// <value></value>
        public string Country { get; set; }

        /// <summary>
        /// The thumbprint of the certificate
        /// </summary>
        /// <value></value>
        public string Thumbprint { get; set; }
    }
}
