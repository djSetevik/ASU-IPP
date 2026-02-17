namespace ASUIPP.Core.Helpers
{
    public static class NameHelper
    {
        /// <summary>
        /// "Иванов Иван Иванович" → "Иванов И.И."
        /// </summary>
        public static string ToShortName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "";

            var parts = fullName.Trim().Split(' ');

            if (parts.Length == 1)
                return parts[0];

            if (parts.Length == 2)
                return $"{parts[0]} {parts[1][0]}.";

            return $"{parts[0]} {parts[1][0]}.{parts[2][0]}.";
        }
    }
}