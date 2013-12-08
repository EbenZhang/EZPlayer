using System.ComponentModel;
using System.Diagnostics;
using System;
using System.Linq.Expressions;

namespace EZPlayer.ViewModel
{
    public class ViewModelBase<TViewModel> : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChange<TProperty>(Expression<Func<TViewModel, TProperty>> property)
        {
            string propertyName = ((MemberExpression)property.Body).Member.Name;

            this.VerifyPropertyName(propertyName);

            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid property name: " + propertyName;

                throw new Exception(msg);
            }
        }
    }
}
