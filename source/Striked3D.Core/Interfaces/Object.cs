using Striked3D.Core.Reference;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Striked3D.Core
{
    public abstract class Object : IObject, INotifyPropertyChanged
    {
  

        private Guid id = Guid.NewGuid();
        public Guid Id { get { return id; } }

        public Object()
        {
        }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);  
        }

        public Dictionary<string, Type> GetExportProperties()
        {
            var dic = new Dictionary<string, Type>();

            foreach (var prop in this.GetType().GetProperties())
            {
                if (prop.GetCustomAttributes(typeof(ExportAttribute), true).Length > 0)
                {
                    dic.Add(prop.Name, prop.PropertyType);
                }
            }

            return dic;
        }

        public T GetValue<T>(string propName)
        {
            return (T)this.GetType().GetProperty(propName).GetValue(this, null);
        }
        public Type GetValueType(string propName)
        {
            return this.GetType().GetProperty(propName).PropertyType;
        }
    
  
        public  void SetValue(string propertyName, object propertyVal)
        {
            //find out the type
            Type type = this.GetType();

            //get the property information based on the type
            System.Reflection.PropertyInfo propertyInfo = type.GetProperty(propertyName);

            //Set the value of the property
            propertyInfo.SetValue(this, propertyVal, null);
        }

        public object GetValue(string propName)
        {
            return this.GetType().GetProperty(propName).GetValue(this, null);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected void SetProperty<T>(string propertyName, ref T field, T newValue)
        {
            if (!EqualityComparer<T>.Default.Equals(field, newValue))
            {
                field = newValue;
                OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
