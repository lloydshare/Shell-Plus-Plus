#region Using
using System.Collections;
using System.Reflection;
using System.Text;
using System.Xml;
#endregion

/// <summary>
/// Custom XML Serializer
/// </summary>
public static class XML
{
    /// <summary>
    /// Serialize any objects Fields and Properties to XML file - Handles nulls and arrays of objects
    /// </summary>
    /// <param name="obj">Object to serialize</param>
    /// <param name="fileName">path and file name for out-put XML file</param>
    /// <param name="bindingFlags">Custom binding flags i.e  new BindingFlags[] { BindingFlags.Public, BindingFlags.NonPublic, BindingFlags.Instance }</param>
    /// <returns>Returns any errors</returns>
    public static string Serialize(object obj, string fileName, BindingFlags[] bindingFlags)
    {
        try
        {
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan))
            using (var xtw = XmlWriter.Create(fs, new XmlWriterSettings { Indent = true, IndentChars = "  ", Encoding = Encoding.UTF8 }))
            {
                SerializeObject(obj, xtw, bindingFlags);
                xtw.Flush();
            }
            return null; // Success, no error
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
    // Add excluded types here
    private static readonly HashSet<Type> ExcludedTypes = new HashSet<Type>
    {
        typeof(System.Windows.Forms.TextBox),
        typeof(System.Windows.Forms.Panel),
        typeof(System.Drawing.Bitmap),
        typeof(System.Windows.Forms.Timer)
        // Add more types as needed
    };

    private static void SerializeObject(object obj, XmlWriter xtw, BindingFlags[] bindingFlags, string elementName = null)
    {
        if (obj == null)
        {
            xtw.WriteStartElement(elementName ?? "Null");
            xtw.WriteAttributeString("type", "null");
            xtw.WriteEndElement();
            return;
        }

        Type type = obj.GetType();
        elementName = elementName ?? type.Name;

        if (ExcludedTypes.Contains(type))
        {
            xtw.WriteStartElement(elementName);
            xtw.WriteAttributeString("type", type.FullName);
            xtw.WriteAttributeString("excluded", "true");
            xtw.WriteEndElement();
            return;
        }

        xtw.WriteStartElement(elementName);
        xtw.WriteAttributeString("type", type.FullName);

        // Handle arrays
        if (type.IsArray)
        {
            Array arr = (Array)obj;
            foreach (var item in arr)
            {
                SerializeObject(item, xtw, bindingFlags, "Item");
            }
        }
        // Handle List<T>
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var list = (IEnumerable)obj;
            foreach (var item in list)
            {
                SerializeObject(item, xtw, bindingFlags, "Item");
            }
        }
        else if (type.IsClass && type != typeof(string) || type.IsValueType && !type.IsPrimitive && !type.IsEnum && type != typeof(DateTime))
        {
            BindingFlags combinedFlags = BindingFlags.Default;
            if (bindingFlags != null && bindingFlags.Length > 0)
            {
                foreach (var flag in bindingFlags)
                    combinedFlags |= flag;
            }
            else
            {
                combinedFlags = BindingFlags.Public | BindingFlags.Instance;
            }

            // Serialize fields
            FieldInfo[] fields = type.GetFields(combinedFlags);
            foreach (FieldInfo field in fields)
            {
                object val = field.GetValue(obj);
                SerializeObject(val, xtw, bindingFlags, field.Name);
            }

            // Serialize properties
            PropertyInfo[] properties = type.GetProperties(combinedFlags);
            foreach (PropertyInfo property in properties)
            {
                if (property.CanRead && property.GetIndexParameters().Length == 0) // Exclude indexed properties
                {
                    object val = property.GetValue(obj);
                    SerializeObject(val, xtw, bindingFlags, property.Name);
                }
            }
        }
        else
        {
            xtw.WriteString(obj.ToString());
        }

        xtw.WriteEndElement();
    }

    /// <summary>
    /// De-Serialize any objects Fields and Properties from XML file
    /// </summary>
    /// <param name="type">Object type to De-Serialize</param>
    /// <param name="fileName">Input path and file name</param>
    /// <param name="bindingFlags">Custom binding flags i.e  new BindingFlags[] { BindingFlags.Public, BindingFlags.NonPublic, BindingFlags.Instance }</param>
    /// <returns></returns>
    public static (object result, string error) DeSerialize(Type type, string fileName, BindingFlags[] bindingFlags)
    {
        //try
        //{
            // Combine flags
            BindingFlags combinedFlags = BindingFlags.Default;
            if (bindingFlags != null && bindingFlags.Length > 0)
            {
                foreach (var flag in bindingFlags)
                    combinedFlags |= flag;
            }
            else
            {
                combinedFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            }

            object obj = Activator.CreateInstance(type, nonPublic: true);

            // Fast lookup by field and property name for the root type
            var rootFields = type.GetFields(combinedFlags)
                                 .ToDictionary(f => f.Name, f => f, StringComparer.Ordinal);
            var rootProperties = type.GetProperties(combinedFlags)
                                    .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0)
                                    .ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);

            var xrSettings = new XmlReaderSettings
            {
                IgnoreWhitespace = true,
                IgnoreComments = true
            };

            // Reference map: id -> object (only for reference types)
            var idMap = new Dictionary<int, object>();

            // Cache fields and properties per type for complex objects
            var fieldsByType = new Dictionary<Type, Dictionary<string, FieldInfo>>();
            var propertiesByType = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

            static Type ResolveTypeOrFallback(string? typeName, Type fallback)
            {
                if (string.IsNullOrEmpty(typeName)) return fallback;

                // Try Type.GetType first
                var t = Type.GetType(typeName, throwOnError: false);
                if (t != null) return t;

                // Search loaded assemblies
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        t = asm.GetType(typeName, throwOnError: false, ignoreCase: false);
                        if (t != null) return t;
                    }
                    catch { /* ignore */ }
                }

                return fallback;
            }

            object? ReadObject(XmlReader xr, Type declaredType)
            {
                // At element start
                string? typeAttr = xr.GetAttribute("type");
                string? isNullAttr = xr.GetAttribute("isNull");
                string? refAttr = xr.GetAttribute("ref");
                string? idAttr = xr.GetAttribute("id");

                bool isEmpty = xr.IsEmptyElement;

                // Determine actual type to create/convert to
                Type actualType = ResolveTypeOrFallback(typeAttr, declaredType);

                // Null handling
                if (string.Equals(isNullAttr, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(typeAttr, "null", StringComparison.OrdinalIgnoreCase))
                {
                    xr.ReadStartElement(); // consumes element (empty)
                    return null;
                }

                // Reference handling: <X ref="n" />
                if (!string.IsNullOrEmpty(refAttr))
                {
                    xr.ReadStartElement();
                    if (!int.TryParse(refAttr, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int refId))
                        throw new InvalidDataException($"Invalid ref id '{refAttr}'.");
                    if (!idMap.TryGetValue(refId, out var referenced))
                        throw new InvalidDataException($"Dangling ref id '{refId}' not found.");
                    return referenced;
                }

                // Arrays: <Field type="T[]" id="n"><Item .../></Field>
                if (actualType.IsArray)
                {
                    var elemType = actualType.GetElementType()!;
                    var items = new List<object?>();

                    // Check for self-closing (empty) array element BEFORE ReadStartElement
                    if (xr.IsEmptyElement)
                    {
                        xr.ReadStartElement(); // consume the empty element
                        var arr = Array.CreateInstance(elemType, 0);

                        // Register id if present
                        if (!string.IsNullOrEmpty(idAttr) &&
                            int.TryParse(idAttr, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int newId))
                        {
                            idMap[newId] = arr;
                        }

                        return arr;
                    }

                    xr.ReadStartElement(); // into <Field> or <ContentStruct>

                    while (xr.NodeType == XmlNodeType.Element && xr.Name == "Item")
                    {
                        var item = ReadObject(xr, elemType);
                        items.Add(item);
                    }

                    xr.ReadEndElement();

                    var arr2 = Array.CreateInstance(elemType, items.Count);
                    for (int i = 0; i < items.Count; i++)
                        arr2.SetValue(items[i], i);

                    if (!string.IsNullOrEmpty(idAttr) &&
                        int.TryParse(idAttr, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int newId2))
                    {
                        idMap[newId2] = arr2;
                    }

                    return arr2;
                }

                // Handle List<T>
                if (actualType.IsGenericType && actualType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elemType = actualType.GetGenericArguments()[0];
                    var listInstance = (IList)Activator.CreateInstance(actualType)!;

                    if (xr.IsEmptyElement)
                    {
                        xr.ReadStartElement();
                        if (!string.IsNullOrEmpty(idAttr) &&
                            int.TryParse(idAttr, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int newId))
                        {
                            idMap[newId] = listInstance;
                        }
                        return listInstance;
                    }

                    xr.ReadStartElement();

                    while (xr.NodeType == XmlNodeType.Element && xr.Name == "Item")
                    {
                        var item = ReadObject(xr, elemType);
                        listInstance.Add(item);
                    }

                    xr.ReadEndElement();

                    if (!string.IsNullOrEmpty(idAttr) &&
                        int.TryParse(idAttr, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int newId2))
                    {
                        idMap[newId2] = listInstance;
                    }

                    return listInstance;
                }

                // If element is empty and not null/ref: treat as default/empty content
                if (isEmpty)
                {
                    xr.ReadStartElement(); // consumes the empty element
                    var emptyValue = ConvertFromStringInvariant(string.Empty, actualType);
                    if (!string.IsNullOrEmpty(idAttr) &&
                        !actualType.IsValueType &&
                        int.TryParse(idAttr, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int newId))
                    {
                        if (emptyValue != null)
                            idMap[newId] = emptyValue;
                    }
                    return emptyValue;
                }

                // Peek inside content to decide scalar vs complex
                xr.ReadStartElement(); // now inside the element

                if (xr.NodeType == XmlNodeType.Text || xr.NodeType == XmlNodeType.CDATA || xr.NodeType == XmlNodeType.SignificantWhitespace)
                {
                    // Scalar content
                    string text = xr.ReadContentAsString();

                    // Close </ElementName>
                    xr.ReadEndElement();

                    var converted = ConvertFromStringInvariant(text, actualType);

                    // For reference types (e.g., string) with id attribute, register the instance
                    if (!string.IsNullOrEmpty(idAttr) &&
                        !actualType.IsValueType &&
                        int.TryParse(idAttr, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int newId))
                    {
                        if (converted != null)
                            idMap[newId] = converted;
                    }

                    return converted;
                }
                else
                {
                    // Complex object with nested fields or properties
                    object instance = Activator.CreateInstance(actualType, nonPublic: true);

                    // Register id early to allow self-references/cycles
                    if (!string.IsNullOrEmpty(idAttr) &&
                        int.TryParse(idAttr, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int newId))
                    {
                        idMap[newId] = instance;
                    }

                    if (!fieldsByType.TryGetValue(actualType, out var fieldMap))
                    {
                        fieldMap = actualType.GetFields(combinedFlags)
                                             .ToDictionary(f => f.Name, f => f, StringComparer.Ordinal);
                        fieldsByType[actualType] = fieldMap;
                    }

                    if (!propertiesByType.TryGetValue(actualType, out var propertyMap))
                    {
                        propertyMap = actualType.GetProperties(combinedFlags)
                                               .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0)
                                               .ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);
                        propertiesByType[actualType] = propertyMap;
                    }

                    // Process nested elements until we reach the end tag
                    while (xr.NodeType != XmlNodeType.EndElement)
                    {
                        if (xr.NodeType == XmlNodeType.Element)
                        {
                            string childName = xr.Name;

                            if (fieldMap.TryGetValue(childName, out var fieldInfo))
                            {
                                var fieldValue = ReadObject(xr, fieldInfo.FieldType);
                                fieldInfo.SetValue(instance, fieldValue);
                            }
                            else if (propertyMap.TryGetValue(childName, out var propertyInfo))
                            {
                                var propertyValue = ReadObject(xr, propertyInfo.PropertyType);
                                propertyInfo.SetValue(instance, propertyValue);
                            }
                            else
                            {
                                xr.Skip(); // Skip unrecognized elements
                            }
                        }
                        else
                        {
                            xr.Read(); // Advance past non-element nodes (e.g., whitespace)
                        }
                    }

                    // Now on </ElementName>
                    xr.ReadEndElement(); // Close </ElementName>
                    return instance;
                }
            }

            using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var xr = XmlReader.Create(fs, xrSettings);

            xr.MoveToContent();
            xr.ReadStartElement(); // root

            while (xr.NodeType != XmlNodeType.EndElement)
            {
                if (xr.NodeType == XmlNodeType.Element)
                {
                    string memberName = xr.Name;

                    if (rootFields.TryGetValue(memberName, out var field))
                    {
                        var value = ReadObject(xr, field.FieldType);
                        field.SetValue(obj, value);
                    }
                    else if (rootProperties.TryGetValue(memberName, out var property))
                    {
                        var value = ReadObject(xr, property.PropertyType);
                        property.SetValue(obj, value);
                    }
                    else
                    {
                        xr.Skip();
                    }
                }
                else
                {
                    xr.Read(); // Advance past non-element nodes
                }
            }

            xr.ReadEndElement(); // Close </EDXMLFile>

            return (obj, null);
        //}
        //catch (Exception ex)
        //{
        //    return (null, ex.ToString());
        //}
    }

    /// <summary>
    /// Generic convenience overload of DeSerialize
    /// </summary>
    /// <typeparam name="T">Object to De-Serialize</typeparam>
    /// <param name="fileName">Input path and file name</param>
    /// <param name="bindingFlags">Custom binding flags i.e  new BindingFlags[] { BindingFlags.Public, BindingFlags.NonPublic, BindingFlags.Instance }</param>
    /// <returns></returns>
    public static (T result, string error) DeSerialize<T>(string fileName, BindingFlags[] bindingFlags) where T : class
    {
        var (obj, err) = DeSerialize(typeof(T), fileName, bindingFlags);
        return (obj as T, err);
    }

    private static object? ConvertFromStringInvariant(string s, Type targetType)
    {
        // Handle Nullable<T>
        var underlying = Nullable.GetUnderlyingType(targetType);
        if (underlying != null)
        {
            if (string.IsNullOrEmpty(s)) return null;
            targetType = underlying;
        }

        // Enums
        if (targetType.IsEnum)
        {
            try { return Enum.Parse(targetType, s, ignoreCase: true); }
            catch { return Activator.CreateInstance(targetType); }
        }

        // Use TypeConverter with invariant culture when possible
        var converter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
        try
        {
            if (converter != null)
            {
                // Prefer invariant culture conversion
                if (converter is System.ComponentModel.TypeConverter tc)
                {
                    // ConvertFromInvariantString handles numbers with '.' etc.
                    var inv = tc.ConvertFromInvariantString(s);
                    if (inv != null) return inv;
                }

                if (converter.CanConvertFrom(typeof(string)))
                {
                    return converter.ConvertFrom(null, System.Globalization.CultureInfo.InvariantCulture, s);
                }
            }

            // Fallback
            return Convert.ChangeType(s, targetType, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }
    }
}
