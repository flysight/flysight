using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Brejc.Common
{
    public class ObjectToStringFormatter
    {
    }

    public sealed class ObjectHelper
    {
        static public string ObjectToString (object value)
        {
            return ObjectToString (value, null);
        }

        [SuppressMessage ("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "formatter")]
        static public string ObjectToString (object value, ObjectToStringFormatter formatter)
        {
            if (value == null)
                throw new ArgumentNullException ("value");                
            
            StringBuilder toString = new StringBuilder ();

            // append the class name
            toString.Append (value.GetType ().Name);
            toString.Append (": ");

            // append object properties' values
            string comma = null;
            foreach (PropertyInfo property in value.GetType ().GetProperties ())
            {
                ParameterInfo[] indexParameters = property.GetIndexParameters ();
                if (indexParameters.Length > 0)
                    continue;

                bool skip = false;
                foreach (Type interfaceType in property.PropertyType.GetInterfaces ())
                {
                    if (interfaceType == typeof (ICollection))
                    {
                        skip = true;
                        break;
                    }
                }

                if (skip)
                    continue;

                object propertyValue = property.GetValue (value, null);

                if (propertyValue == null)
                    continue;

                toString.AppendFormat (System.Globalization.CultureInfo.InvariantCulture,
                    "{0}{1} = '{2}'", comma, property.Name, propertyValue);
                comma = ", ";
            }

            return toString.ToString ();
        }

        /// <summary>
        /// Determines whether two collections are equal. They are equal if they contain the equal elements in the same order.
        /// </summary>
        /// <param name="collection1">The first collection.</param>
        /// <param name="collection2">The second collection.</param>
        /// <returns><c>true</c> if the collections are equal; <c>false</c> otherwise.</returns>
        static public bool AreCollectionsEqual (ICollection collection1, ICollection collection2)
        {
            if (collection1 == null && collection2 == null)
                return true;

            if (collection1 == null || collection2 == null)
                return false;

            if (collection1.Count != collection2.Count)
                return false;

            IEnumerator enumerator1 = collection1.GetEnumerator ();
            IEnumerator enumerator2 = collection2.GetEnumerator ();

            while (true)
            {
                bool hasElement1 = enumerator1.MoveNext ();
                bool hasElement2 = enumerator2.MoveNext ();

                if (hasElement1 == false || hasElement2 == false)
                    break;

                if (false == Comparer.Equals (enumerator1.Current, enumerator2.Current))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// If the specified string value is not <c>null</c>, the method XORs the specified hash code
        /// with the hash code of the string value and returns the result.
        /// </summary>
        /// <param name="hashCode">The existing hash code.</param>
        /// <param name="value">String value.</param>
        /// <returns>A new hash code.</returns>
        static public int XorHashCode (int hashCode, string value)
        {
            if (value != null)
                hashCode ^= value.GetHashCode ();

            return hashCode;
        }

        private ObjectHelper () { } 
    }
}
