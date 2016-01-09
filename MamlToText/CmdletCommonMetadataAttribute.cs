namespace System.Management.Automation
{
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class CmdletCommonMetadataAttribute : CmdletMetadataAttribute
    {
        protected CmdletCommonMetadataAttribute()
        {
        }

        public string DefaultParameterSetName { get; set; }
        public string HelpUri { get; set; }
        public bool SupportsShouldProcess { get; set; }
    }
}
