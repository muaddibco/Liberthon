using PostSharp.Aspects;
using PostSharp.Reflection;
using System;
using Wist.Client.Common.Exceptions;
using Wist.Client.Common.Mvvm.ViewModels;

namespace Wist.Client.Common.Aspects
{
    /// <summary>
    /// Attribute decorating properties allowing automatic OnPropertyChanged notification when Property's values has changed. 
    /// Requires that class declaring decorated property will inherit from <see cref="Savyon.Diagnostics.Common.Mvvm.ViewModels.ViewModelBase"/> class
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Property)]
    [LinesOfCodeAvoided(5)]
    public sealed class BindablePropertyAttribute : LocationInterceptionAspect
    {
        private string _propertyName;

        public override void CompileTimeInitialize(LocationInfo targetLocation, AspectInfo aspectInfo)
        {
            base.CompileTimeInitialize(targetLocation, aspectInfo);

            _propertyName = targetLocation.Name;
        }

        public override bool CompileTimeValidate(LocationInfo locationInfo)
        {
            if (!typeof(ViewModelBase).IsAssignableFrom(locationInfo.DeclaringType))
                throw new WrongBindableInheritanceException(locationInfo.DeclaringType.FullName);

            return true;
        }

        public override void OnSetValue(LocationInterceptionArgs args)
        {
            bool isChanging = args.GetCurrentValue() != args.Value;

            args.ProceedSetValue();

            if (args.Instance is ViewModelBase vm && isChanging)
                vm.OnPropertyChanged(args.LocationName);
        }
    }
}
