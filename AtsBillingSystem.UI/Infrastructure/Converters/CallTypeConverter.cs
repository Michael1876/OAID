using System;
using System.Globalization;
using System.Windows.Data;
using AtsBillingSystem.Domain.Enums;

namespace AtsBillingSystem.UI.Infrastructure.Converters
{
    public class CallTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CallType callType)
            {
                return callType switch
                {
                    CallType.Internal => "Внутрисетевой",
                    CallType.City => "Городской (внешний)",
                    _ => "Неизвестно"
                };
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Этот метод нужен только если мы редактируем данные через UI.
            // Так как у нас DataGrid IsReadOnly="True", здесь можно просто выкинуть исключение.
            throw new NotSupportedException();
        }
    }
}