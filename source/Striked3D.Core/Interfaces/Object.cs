using BinaryPack.Attributes;
using BinaryPack.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Striked3D.Core
{
    [BinarySerialization(SerializationMode.Properties | SerializationMode.NonPublicMembers)]
    public abstract class Object : IObject, INotifyPropertyChanged
    {
        private Guid id = Guid.NewGuid();

        public virtual Guid Id { get { return id; } set { id = value; } }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public Dictionary<string, Type> GetExportProperties()
        {
            Dictionary<string, Type> dic = new Dictionary<string, Type>();

            foreach (System.Reflection.PropertyInfo prop in GetType().GetProperties())
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
            return (T)GetType().GetProperty(propName).GetValue(this, null);
        }
        public Type GetValueType(string propName)
        {
            return GetType().GetProperty(propName).PropertyType;
        }


        public void SetValue(string propertyName, object propertyVal)
        {
            //find out the type
            Type type = GetType();

            //get the property information based on the type
            System.Reflection.PropertyInfo propertyInfo = type.GetProperty(propertyName);

            //Set the value of the property
            propertyInfo.SetValue(this, propertyVal, null);
        }

        public object GetValue(string propName)
        {
            return GetType().GetProperty(propName).GetValue(this, null);
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
