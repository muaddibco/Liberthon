using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Wist.Client.Common.Mvvm.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public void OnPropertyChanged(Expression<Func<object>> action)
        {
            string propertyName;

            UnaryExpression body = action.Body as UnaryExpression;
            if (body != null)
            {
                UnaryExpression expression = body;
                propertyName = ((MemberExpression)expression.Operand).Member.Name;
            }
            else
            {
                MemberExpression expression = (MemberExpression)action.Body;
                propertyName = expression.Member.Name;
            }

            OnPropertyChanged(propertyName);
        }

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
