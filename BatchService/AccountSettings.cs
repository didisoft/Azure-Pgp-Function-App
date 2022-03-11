// https://github.com/Azure-Samples/azure-batch-samples/blob/master/CSharp/Common/AccountSettings.cs

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Batch.Common
{
    public class AccountSettings
    {
        public string BatchServiceUrl { get; set; }
        public string BatchAccountName { get; set; }
        public string BatchAccountKey { get; set; }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            AddSetting(stringBuilder, "BatchAccountName", this.BatchAccountName);
            AddSetting(stringBuilder, "BatchAccountKey", this.BatchAccountKey);
            AddSetting(stringBuilder, "BatchServiceUrl", this.BatchServiceUrl);

            return stringBuilder.ToString();
        }

        private static void AddSetting(StringBuilder stringBuilder, string settingName, object settingValue)
        {
            stringBuilder.AppendFormat("{0} = {1}", settingName, settingValue).AppendLine();
        }
    }
}