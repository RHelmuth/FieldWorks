﻿// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class groups the static methods used for generating XHTML, according to specified configurations, from Fieldworks model objects
	/// </summary>
	public static class ConfiguredXHTMLGenerator
	{
		/// <summary>
		/// The Assembly that the model Types should be loaded from. Allows test code to introduce a test model.
		/// </summary>
		internal static string AssemblyFile { get; set; }

		/// <summary>
		/// Map of the Assembly to the file name, so that different tests can use different models
		/// </summary>
		internal static Dictionary<string, Assembly> AssemblyMap = new Dictionary<string, Assembly>();

		private const string PublicIdentifier = @"-//W3C//DTD XHTML 1.1//EN";

		/// <summary>
		/// Static initializer setting the AssemblyFile to the default Fieldworks model dll.
		/// </summary>
		static ConfiguredXHTMLGenerator()
		{
			AssemblyFile = "FDO";
		}

		/// <summary>
		/// Generates self-contained XHTML for a single entry for, eg, the preview panes in Lexicon Edit and the Dictionary Config dialog
		/// </summary>
		/// <returns>The HTML as a string</returns>
		public static string GenerateEntryHtmlWithStyles(ICmObject entry, DictionaryConfigurationModel configuration,
																		 DictionaryPublicationDecorator pubDecorator, Mediator mediator)
		{
			if(entry == null)
			{
				throw new ArgumentNullException("entry");
			}
			if(pubDecorator == null)
			{
				throw new ArgumentException("pubDecorator");
			}
			var projectPath = Path.Combine(FdoFileHelper.GetConfigSettingsDir(entry.Cache.ProjectId.ProjectFolder),
													 DictionaryConfigurationListener.GetDictionaryConfigurationType(mediator));
			var previewCssPath = Path.Combine(projectPath, "Preview.css");
			var stringBuilder = new StringBuilder();
			using(var writer = XmlWriter.Create(stringBuilder))
			using(var cssWriter = new StreamWriter(previewCssPath, false))
			{
				GenerateOpeningHtml(writer, previewCssPath);
				GenerateXHTMLForEntry(entry, configuration, pubDecorator, writer, (FdoCache)mediator.PropertyTable.GetValue("cache"));
				GenerateClosingHtml(writer);
				writer.Flush();
				cssWriter.Write(CssGenerator.GenerateCssFromConfiguration(configuration, mediator));
				cssWriter.Flush();
			}

			return stringBuilder.ToString();
		}

		private static void GenerateOpeningHtml(XmlWriter xhtmlWriter, string cssPath)
		{
			xhtmlWriter.WriteDocType("html", PublicIdentifier, null, null);
			xhtmlWriter.WriteStartElement("html", "http://www.w3.org/1999/xhtml");
			xhtmlWriter.WriteAttributeString("lang", "utf-8");
			xhtmlWriter.WriteStartElement("head");
			xhtmlWriter.WriteStartElement("link");
			xhtmlWriter.WriteAttributeString("href", "file:///" + cssPath);
			xhtmlWriter.WriteAttributeString("rel", "stylesheet");
			xhtmlWriter.WriteEndElement(); //</link>
			xhtmlWriter.WriteEndElement(); //</head>
			xhtmlWriter.WriteStartElement("body");
		}

		private static void GenerateClosingHtml(XmlWriter xhtmlWriter)
		{
			xhtmlWriter.WriteEndElement(); //</body>
			xhtmlWriter.WriteEndElement(); //</html>
		}

		/// <summary>
		/// Saves the generated content into the given xhtml and css file paths for all the entries in
		/// the given collection.
		/// </summary>
		public static void SavePublishedHtmlWithStyles(IEnumerable<int> entryHvos, DictionaryPublicationDecorator publicationDecorator, DictionaryConfigurationModel configuration, Mediator mediator, string xhtmlPath, string cssPath, IThreadedProgress progress = null)
		{
			var cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			using(var xhtmlWriter = XmlWriter.Create(xhtmlPath))
			using(var cssWriter = new StreamWriter(cssPath, false))
			{
				GenerateOpeningHtml(xhtmlWriter, cssPath);
				string lastHeader = null;
				foreach(var hvo in entryHvos)
				{
					var entry = cache.ServiceLocator.GetObject(hvo);
					// TODO pH 2014.08: generate only if entry is published (confignode enabled, pubAsMinor, selected complex- or variant-form type)
					GenerateLetterHeaderIfNeeded(entry, ref lastHeader, xhtmlWriter, cache);
					GenerateXHTMLForEntry(entry, configuration, publicationDecorator, xhtmlWriter, cache);
					if(progress != null)
					{
						progress.Position++;
					}
				}
				GenerateClosingHtml(xhtmlWriter);
				xhtmlWriter.Flush();
				cssWriter.Write(CssGenerator.GenerateLetterHeaderCss(mediator));
				cssWriter.Write(CssGenerator.GenerateCssFromConfiguration(configuration, mediator));
				cssWriter.Flush();
			}
		}

		internal static void GenerateLetterHeaderIfNeeded(ICmObject entry, ref string lastHeader, XmlWriter xhtmlWriter, FdoCache cache)
		{
			var lexEntry = entry as ILexEntry;
			// If performance is an issue these dummy's can be stored between calls
			var dummyOne = new Dictionary<string, Set<string>>();
			var dummyTwo = new Dictionary<string, Dictionary<string, string>>();
			var dummyThree = new Dictionary<string, Set<string>>();
			var wsString = cache.WritingSystemFactory.GetStrFromWs(cache.DefaultVernWs);
			var firstLetter = ConfiguredExport.GetLeadChar(lexEntry.HeadWord.Text, wsString,
																		  dummyOne, dummyTwo, dummyThree, cache);
			if(firstLetter != lastHeader && !String.IsNullOrEmpty(firstLetter))
			{
				var headerTextBuilder = new StringBuilder();
				headerTextBuilder.Append(Icu.ToTitle(firstLetter, wsString));
				headerTextBuilder.Append(' ');
				headerTextBuilder.Append(firstLetter.Normalize());

				xhtmlWriter.WriteStartElement("div");
				xhtmlWriter.WriteAttributeString("class", "letHead");
				xhtmlWriter.WriteStartElement("div");
				xhtmlWriter.WriteAttributeString("class", "letter");
				xhtmlWriter.WriteString(headerTextBuilder.ToString());
				xhtmlWriter.WriteEndElement();
				xhtmlWriter.WriteEndElement();

				lastHeader = firstLetter;
			}
		}

		/// <summary>
		/// Generating the xhtml representation for the given ICmObject using the given configuration to select which data to write out
		/// If it is a Dictionary Main Entry or non-Dictionary entry, uses the first configuration.
		/// If it is a Minor Entry, first checks whether the entry should be published as a Minor Entry; then, generates XHTML for each applicable
		/// Minor Entry configuration.
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="configuration"></param>
		/// <param name="publicationDecorator"></param>
		/// <param name="writer"></param>
		/// <param name="cache"></param>
		public static void GenerateXHTMLForEntry(ICmObject entry, DictionaryConfigurationModel configuration,
			DictionaryPublicationDecorator publicationDecorator, XmlWriter writer, FdoCache cache)
		{
				if (IsMinorEntry(entry))
				{
					if (((ILexEntry)entry).PublishAsMinorEntry)
						for (var i = 1; i < configuration.Parts.Count; i++)
							GenerateXHTMLForEntry(entry, configuration.Parts[i], publicationDecorator, writer, cache);
							// TODO pH 2014.08: determine whether each config is applicable (FormType is checked in the Minor Entry Options)
				}
				else
				{
					GenerateXHTMLForEntry(entry, configuration.Parts[0], publicationDecorator, writer, cache);
				}
		}

		internal static bool IsMinorEntry(ICmObject entry)
		{
			// owning an ILexEntryRef denotes a minor entry (Complex* or Variant Form)
			return entry is ILexEntry && ((ILexEntry)entry).EntryRefsOS.Any();
			// TODO pH 2014.08: *Owning a LexEntryRef denotes a minor entry only in those configs that display complex forms as subentries
			// TODO				(Root, Bart, and their descendants) or if the reftype is Variant Form
		}

		/// <summary>Generates XHTML for an ICmObject for a specific ConfigurableDictionaryNode</summary>
		/// <param name="entry"></param>
		/// <param name="configuration"><remarks>this configuration node must match the entry type</remarks></param>
		/// <param name="publicationDecorator"></param>
		/// <param name="writer"></param>
		/// <param name="cache"></param>
		internal static void GenerateXHTMLForEntry(ICmObject entry, ConfigurableDictionaryNode configuration, DictionaryPublicationDecorator publicationDecorator, XmlWriter writer, FdoCache cache)
		{
			if(writer == null || entry == null || configuration == null || cache == null)
			{
				throw new ArgumentNullException();
			}
			if(String.IsNullOrEmpty(configuration.FieldDescription))
			{
				throw new ArgumentException(@"Invalid configuration: FieldDescription can not be null", @"configuration");
			}
			if(entry.ClassID != cache.MetaDataCacheAccessor.GetClassId(configuration.FieldDescription))
			{
				throw new ArgumentException(@"The given argument doesn't configure this type", @"configuration");
			}
			if(!configuration.IsEnabled)
			{
				return;
			}

			writer.WriteStartElement("div");
			WriteClassNameAttribute(writer, configuration);
			writer.WriteAttributeString("id", "hvo" + entry.Hvo);
			foreach(var config in configuration.Children)
			{
				GenerateXHTMLForFieldByReflection(entry, config, publicationDecorator, writer, cache);
			}
			writer.WriteEndElement(); // </div>
		}

		/// <summary>
		/// This method will write out the class name attribute into the xhtml for the given configuration node
		/// taking into account the current information in ClassNameOverrides
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="configNode">used to look up any mapping overrides</param>
		private static void WriteClassNameAttribute(XmlWriter writer, ConfigurableDictionaryNode configNode)
		{
			writer.WriteAttributeString("class", CssGenerator.GetClassAttributeForConfig(configNode));
		}

		/// <summary>
		/// This method will use reflection to pull data out of the given object based on the given configuration and
		/// write out appropriate XHTML.
		/// </summary>
		/// <param name="field"></param>
		/// <param name="config"></param>
		/// <param name="publicationDecorator"></param>
		/// <param name="writer"></param>
		/// <param name="cache"></param>
		private static void GenerateXHTMLForFieldByReflection(object field, ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator, XmlWriter writer, FdoCache cache)
		{
			if(!config.IsEnabled)
			{
				return;
			}
			var entryType = field.GetType();
			object propertyValue = null;
			if(config.IsCustomField)
			{
				// TODO: Get the class name string to use dynamically so that fields on sense, example sentence, and allomorph work
				var customFieldFlid = cache.MetaDataCacheAccessor.GetFieldId("LexEntry", config.FieldDescription, false);
				if(customFieldFlid != 0)
				{
					var customFieldType = cache.MetaDataCacheAccessor.GetFieldType(customFieldFlid);
					switch(customFieldType)
					{
						case (int)CellarPropertyType.ReferenceSequence:
						case (int)CellarPropertyType.OwningSequence:
						{
							var sda = cache.MainCacheAccessor;
							// This method returns the hvo of the object pointed to
							var chvo = sda.get_VecSize(((ICmObject)field).Hvo, customFieldFlid);
							int[] contents;
							using(var arrayPtr = MarshalEx.ArrayToNative<int>(chvo))
							{
								sda.VecProp(((ICmObject)field).Hvo, customFieldFlid, chvo, out chvo, arrayPtr);
								contents = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
							}
							// if the hvo is invalid set propertyValue to null otherwise get the object
							propertyValue = contents.Select(id => cache.LangProject.Services.GetObject(id));
							break;
						}
						case (int)CellarPropertyType.ReferenceAtomic:
						case (int)CellarPropertyType.OwningAtomic:
						{
							// This method returns the hvo of the object pointed to
							propertyValue = cache.MainCacheAccessor.get_ObjectProp(((ICmObject)field).Hvo, customFieldFlid);
							// if the hvo is invalid set propertyValue to null otherwise get the object
							propertyValue = (int)propertyValue > 0 ? cache.LangProject.Services.GetObject((int)propertyValue) : null;
							break;
						}
						case (int)CellarPropertyType.Time:
						{
							propertyValue = SilTime.ConvertFromSilTime(cache.MainCacheAccessor.get_TimeProp(((ICmObject)field).Hvo, customFieldFlid));
							break;
						}
						case (int)CellarPropertyType.MultiUnicode:
						case (int)CellarPropertyType.MultiString:
						{
							propertyValue = cache.MainCacheAccessor.get_MultiStringProp(((ICmObject)field).Hvo, customFieldFlid);
							break;
						}
						case (int)CellarPropertyType.String:
						{
							propertyValue = cache.MainCacheAccessor.get_StringProp(((ICmObject)field).Hvo, customFieldFlid);
							break;
						}
					}
				}
			}
			else
			{
				var property = entryType.GetProperty(config.FieldDescription);
				if(property == null)
				{
					Debug.WriteLine("Issue with finding {0}", (object)config.FieldDescription);
					return;
				}
				propertyValue = property.GetValue(field, new object[] { });
			}
			// If the property value is null there is nothing to generate
			if(propertyValue == null)
			{
				return;
			}
			if(!String.IsNullOrEmpty(config.SubField))
			{
				var subType = propertyValue.GetType();
				var subProp = subType.GetProperty(config.SubField);
				propertyValue = subProp.GetValue(propertyValue, new object[] { });
			}
			var typeForNode = config.IsCustomField
										? GetPropertyTypeFromReflectedTypes(propertyValue.GetType(), null)
										: GetPropertyTypeForConfigurationNode(config);
			switch(typeForNode)
			{
				case(PropertyType.CollectionType):
				{
					if(!IsCollectionEmpty(propertyValue))
					{
						GenerateXHTMLForCollection(propertyValue, config, publicationDecorator, writer, cache);
					}
					return;
				}
				case(PropertyType.MoFormType):
				{
					GenerateXHTMLForMoForm(propertyValue as IMoForm, config, writer, cache);
					return;
				}
				case(PropertyType.CmObjectType):
				{
					GenerateXHTMLForICmObject(propertyValue as ICmObject, config, writer, cache);
					return;
				}
				case (PropertyType.CmPictureType):
				{
					var fileProperty = propertyValue as ICmFile;
					if(fileProperty != null)
					{
						GenerateXHTMLForPicture(fileProperty, config, writer, cache);
					}
					else
					{
						GenerateXHTMLForPictureCaption(propertyValue, config, writer, cache);
					}
					return;
				}
				case(PropertyType.CmPossibility):
				{
					GenerateXHTMLForPossibility(propertyValue, config, publicationDecorator, writer, cache);
					return;
				}
				default:
				{
					GenerateXHTMLForValue(field, propertyValue, config, writer, cache);
					break;
				}
			}

			if(config.Children != null)
			{
				foreach(var child in config.Children)
				{
					GenerateXHTMLForFieldByReflection(propertyValue, child, publicationDecorator, writer, cache);
				}
			}
		}

		private static void GenerateXHTMLForPossibility(object propertyValue,ConfigurableDictionaryNode config,
			DictionaryPublicationDecorator publicationDecorator, XmlWriter writer, FdoCache cache)
		{
			if(config.Children.Any(node => node.IsEnabled))
			{
				writer.WriteStartElement("span");
				writer.WriteAttributeString("class", CssGenerator.GetClassAttributeForConfig(config));
				if(config.Children != null)
				{
					foreach(var child in config.Children)
					{
						GenerateXHTMLForFieldByReflection(propertyValue, child, publicationDecorator, writer, cache);
					}
				}
				writer.WriteEndElement();
			}
		}

		private static void GenerateXHTMLForPictureCaption(object propertyValue, ConfigurableDictionaryNode config, XmlWriter writer, FdoCache cache)
		{
			writer.WriteStartElement("div");
			writer.WriteAttributeString("class", CssGenerator.GetClassAttributeForConfig(config));
			// todo: get sense numbers and captions into the same div and get rid of this if else
			if(config.DictionaryNodeOptions != null)
			{
				GenerateXHTMLForStrings(propertyValue as IMultiString, config, writer, cache);
			}
			else
			{
				GenerateXHTMLForString(propertyValue as ITsString, config, writer, cache);
			}
			writer.WriteEndElement();
		}

		private static void GenerateXHTMLForPicture(ICmFile pictureFile, ConfigurableDictionaryNode config, XmlWriter writer, FdoCache cache)
		{
			writer.WriteStartElement("img");
			writer.WriteAttributeString("class", CssGenerator.GetClassAttributeForConfig(config));
			writer.WriteAttributeString("src", GenerateSrcAttributeFromFilePath(pictureFile));
			writer.WriteAttributeString("id", "hvo" + pictureFile.Hvo);
			writer.WriteEndElement();
		}

		/// <summary>
		/// This method will generate a src attribute which will point to the given file from the xhtml.
		/// TODO: It should return absolute paths when used in the Dictionary preview, but it should use relative paths for export.
		/// </summary>
		private static string GenerateSrcAttributeFromFilePath(ICmFile file)
		{
			var path = file.AbsoluteInternalPath;
			if(Unicode.CheckForNonAsciiCharacters(path))
			{
				// Flex keeps the filename as NFD in memory because it is unicode. We need NFC to actually link to the file
				path = Icu.Normalize(path, Icu.UNormalizationMode.UNORM_NFC);
			}
			return new Uri(path).ToString();
		}

		internal enum PropertyType
		{
			CollectionType,
			MoFormType,
			CmObjectType,
			CmPictureType,
			CmFileType,
			CmPossibility,
			PrimitiveType,
			InvalidProperty
		}

		private static Dictionary<ConfigurableDictionaryNode, PropertyType> _configNodeToTypeMap = new Dictionary<ConfigurableDictionaryNode, PropertyType>();

		/// <summary>
		/// This method will reflectively return the type that represents the given configuration node as
		/// described by the ancestry and FieldDescription and SubField properties of each node in it.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		internal static PropertyType GetPropertyTypeForConfigurationNode(ConfigurableDictionaryNode config)
		{
			Type parentType;
			var fieldType = GetTypeForConfigurationNode(config, out parentType);
			return GetPropertyTypeFromReflectedTypes(fieldType, parentType);
		}

		private static PropertyType GetPropertyTypeFromReflectedTypes(Type fieldType, Type parentType)
		{
			if(fieldType == null)
			{
				return PropertyType.InvalidProperty;
			}
			if(IsCollectionType(fieldType))
			{
				return PropertyType.CollectionType;
			}
			if(typeof(ICmPicture).IsAssignableFrom(parentType))
			{
				return PropertyType.CmPictureType;
			}
			if(typeof(ICmFile).IsAssignableFrom(fieldType))
			{
				return PropertyType.CmFileType;
			}
			if(typeof(IMoForm).IsAssignableFrom(fieldType))
			{
				return PropertyType.MoFormType;
			}
			if(typeof(ICmPossibility).IsAssignableFrom(fieldType))
			{
				return PropertyType.CmPossibility;
			}
			if(typeof(ICmObject).IsAssignableFrom(fieldType))
			{
				return PropertyType.CmObjectType;
			}
			return PropertyType.PrimitiveType;
		}

		internal static Type GetTypeForConfigurationNode(ConfigurableDictionaryNode config, out Type parentType)
		{
			if(config == null)
			{
				throw new ArgumentNullException("config", "The configuration node must not be null.");
			}
			parentType = null;
			var lineage = new Stack<ConfigurableDictionaryNode>();
			// Build a list of the direct line up to the top of the configuration
			lineage.Push(config);
			var next = config;
			while(next.Parent != null)
			{
				next = next.Parent;
				lineage.Push(next);
			}
			// pop off the root configuration and read the FieldDescription property to get our starting point
			var assembly = GetAssemblyForFile(AssemblyFile);
			var rootNode = lineage.Pop();
			var lookupType = assembly.GetType(rootNode.FieldDescription);
			Type fieldType = null;
			if(lookupType == null) // If the FieldDescription didn't load prepend the default model namespace and try again
			{
				lookupType = assembly.GetType("SIL.FieldWorks.FDO.DomainImpl." + rootNode.FieldDescription);
			}
			if(lookupType == null)
			{
				throw new ArgumentException(String.Format(xWorksStrings.InvalidRootConfigurationNode, rootNode.FieldDescription));
			}
			// Traverse the configuration reflectively inspecting the types in parent to child order
			foreach(var node in lineage)
			{
				var property = GetProperty(lookupType, node);
				if(property != null)
				{
					fieldType = property.PropertyType;
				}
				else
				{
					return null;
				}
				if(IsCollectionType(fieldType))
				{
					// When a node points to a collection all the child nodes operate on individual items in the
					// collection, so look them up in the type that the collection contains. e.g. IEnumerable<ILexEntry>
					// gives ILexEntry and IFdoVector<ICmObject> gives ICmObject
					lookupType = fieldType.GetGenericArguments()[0];
				}
				else
				{
					parentType = lookupType;
					lookupType = fieldType;
				}
			}
			return fieldType;
		}

		/// <summary>
		/// Loading an assembly is expensive so we cache the assembly once it has been loaded
		/// for enahanced performance.
		/// </summary>
		private static Assembly GetAssemblyForFile(string assemblyFile)
		{
			if(!AssemblyMap.ContainsKey(assemblyFile))
			{
				AssemblyMap[assemblyFile] = Assembly.Load(AssemblyFile);
			}
			return AssemblyMap[assemblyFile];
		}

		/// <summary>
		/// Return the property info from a given class and node. Will check interface heirarchy for the property
		/// if <code>lookupType</code> is an interface.
		/// </summary>
		/// <param name="lookupType"></param>
		/// <param name="node"></param>
		/// <returns></returns>
		private static PropertyInfo GetProperty(Type lookupType, ConfigurableDictionaryNode node)
		{
			string propertyOfInterest;
			PropertyInfo propInfo;
			var typesToCheck = new Stack<Type>();
			typesToCheck.Push(lookupType);
			do
			{
				var current = typesToCheck.Pop();
				propertyOfInterest = node.FieldDescription;
				// if there is a SubField we need to use the type of the FieldDescription
				// for the rest of this method so set current to the FieldDescription type.
				if(node.SubField != null)
				{
					var property = current.GetProperty(node.FieldDescription);
					propertyOfInterest = node.SubField;
					if(property != null)
					{
						current = property.PropertyType;
					}
				}
				propInfo = current.GetProperty(propertyOfInterest, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if(propInfo == null)
				{
					foreach(var i in current.GetInterfaces())
					{
						typesToCheck.Push(i);
					}
				}
			} while(propInfo == null && typesToCheck.Count > 0);
			return propInfo;
		}

		private static void GenerateXHTMLForMoForm(IMoForm moForm, ConfigurableDictionaryNode config, XmlWriter writer, FdoCache cache)
		{
			// Don't export if there is no such data
			if(moForm == null)
				return;
			GenerateXHTMLForStrings(moForm.Form, config, writer, cache);
			if(config.Children != null && config.Children.Any())
			{
				throw new NotImplementedException("Children for MoForm types not yet supported.");
			}
		}

		/// <summary>
		/// This method will generate the XHTML that represents a collection and its contents
		/// </summary>
		/// <param name="collectionField"></param>
		/// <param name="config"></param>
		/// <param name="publicationDecorator"></param>
		/// <param name="writer"></param>
		/// <param name="cache"></param>
		private static void GenerateXHTMLForCollection(object collectionField, ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator, XmlWriter writer, FdoCache cache)
		{
			writer.WriteStartElement("span");
			WriteClassNameAttribute(writer, config);
			IEnumerable collection;
			if(collectionField is IEnumerable)
			{
				collection = collectionField as IEnumerable;
			}
			else if(collectionField is IFdoVector)
			{
				collection = (collectionField as IFdoVector).Objects;
			}
			else
			{
				throw new ArgumentException("The given field is not a recognized collection");
			}
			var isSingle = collection.Cast<object>().Count() == 1;
			foreach(var item in collection)
			{
				GenerateCollectionItemContent(config, publicationDecorator, writer, cache, item, isSingle);
			}
			writer.WriteEndElement();
		}

		private static void GenerateCollectionItemContent(ConfigurableDictionaryNode config,
																		  DictionaryPublicationDecorator publicationDecorator, XmlWriter writer,
																		  FdoCache cache, object item, bool isSingle)
		{
			// if we are working with senses start wrapping element and write out the sense number sibling item if necessary
			if(config.DictionaryNodeOptions is DictionaryNodeSenseOptions)
			{
				// Wrap the number and sense combination in a sensecontent span so that can both be affected by DisplayEachSenseInParagraph
				writer.WriteStartElement("span");
				writer.WriteAttributeString("class", "sensecontent");
				GenerateSenseNumberSpanIfNeeded(config.DictionaryNodeOptions as DictionaryNodeSenseOptions, writer, item, cache,
														  publicationDecorator, isSingle);
			}

			writer.WriteStartElement(GetElementNameForProperty(config));
			WriteCollectionItemClassAttribute(config, writer);
			if(config.Children != null)
			{
				foreach(var child in config.Children)
					GenerateXHTMLForFieldByReflection(item, child, publicationDecorator, writer, cache);
			}
			writer.WriteEndElement();

			// close out the sense wrapping
			if(config.DictionaryNodeOptions is DictionaryNodeSenseOptions)
			{
				writer.WriteEndElement();
			}
		}

		private static void GenerateSenseNumberSpanIfNeeded(DictionaryNodeSenseOptions senseOptions, XmlWriter writer,
																			 object sense, FdoCache cache,
																			 DictionaryPublicationDecorator publicationDecorator, bool isSingle)
		{
			if(senseOptions == null || (isSingle && !senseOptions.NumberEvenASingleSense))
				return;
			writer.WriteStartElement("span");
			writer.WriteAttributeString("class", "sensenumber");
			writer.WriteString(cache.GetOutlineNumber((ICmObject)sense, LexSenseTags.kflidSenses, false, true, publicationDecorator));
			writer.WriteEndElement();
		}

		private static void GenerateXHTMLForICmObject(ICmObject propertyValue, ConfigurableDictionaryNode config, XmlWriter writer, FdoCache cache)
		{
			// Don't export if there is no such data
			if (propertyValue == null)
				return;
			writer.WriteStartElement("span");
			// Rely on configuration to handle adjusting the classname for "RA" or "OA" model properties
			var fieldDescription = CssGenerator.GetClassAttributeForConfig(config);
			writer.WriteAttributeString("class", fieldDescription);
			if (config.Children != null)
			{
				foreach (var child in config.Children)
				{
					if (child.IsEnabled)
					{
						GenerateXHTMLForFieldByReflection(propertyValue, child, null, writer, cache);
					}
				}
			}

			writer.WriteEndElement();
		}

		/// <summary>
		///  Write out the class element to use in the span for the individual items in the collection
		/// </summary>
		/// <param name="config"></param>
		/// <param name="writer"></param>
		private static void WriteCollectionItemClassAttribute(ConfigurableDictionaryNode config, XmlWriter writer)
		{
			var collectionName = CssGenerator.GetClassAttributeForConfig(config);
			// chop the pluralization off the parent class
			writer.WriteAttributeString("class", collectionName.Substring(0, collectionName.Length - 1).ToLower());
		}

		/// <summary>
		/// This method is used to determine if we need to iterate through a property and generate xhtml for each item
		/// </summary>
		/// <param name="entryType"></param>
		/// <returns></returns>
		private static bool IsCollectionType(Type entryType)
		{
			//Some of our string types smell like collections but don't really act like them, so we handle them seperately
			return !typeof(IMultiStringAccessor).IsAssignableFrom(entryType) && !typeof(String).IsAssignableFrom(entryType) &&
				(typeof(IEnumerable).IsAssignableFrom(entryType) || typeof(IFdoVector).IsAssignableFrom(entryType));
		}

		/// <summary>
		/// Returns true if the given collection is empty (type determined at runtime)
		/// </summary>
		/// <param name="collection"></param>
		/// <exception cref="ArgumentException">if the object given is null, or not a handled collection</exception>
		/// <returns></returns>
		private static bool IsCollectionEmpty(object collection)
		{
			if(collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			if(collection is IEnumerable)
			{
				return !(((IEnumerable)collection).Cast<object>().Any());
			}
			if(collection is IFdoVector)
			{
				return ((IFdoVector)collection).ToHvoArray().Length == 0;
			}
			throw new ArgumentException(@"Can not test something that isn't a collection", "collection");
		}

		/// <summary>
		/// This method generates XHTML content for a given object
		/// </summary>
		/// <param name="field">This is the object that owns the property, needed to look up writing system info for virtual string fields</param>
		/// <param name="propertyValue">data to generate xhtml for</param>
		/// <param name="config"></param>
		/// <param name="writer"></param>
		/// <param name="cache"></param>
		private static void GenerateXHTMLForValue(object field, object propertyValue, ConfigurableDictionaryNode config, XmlWriter writer, FdoCache cache)
		{
			if(propertyValue is ITsString)
			{
				if(!TsStringUtils.IsNullOrEmpty((ITsString)propertyValue))
				{
					writer.WriteStartElement("span");
					WriteClassNameAttribute(writer, config);
					GenerateXHTMLForString((ITsString)propertyValue, config, writer, cache);
					writer.WriteEndElement();
				}
			}
			else if(propertyValue is IMultiStringAccessor)
			{
				GenerateXHTMLForStrings((IMultiStringAccessor)propertyValue, config,
											  writer, cache);
			}
			else if(propertyValue is int)
			{
				WriteElementContents(propertyValue, config, writer);
			}
			else if(propertyValue is DateTime)
			{
				WriteElementContents(((DateTime)propertyValue).ToLongDateString(), config, writer);
			}
			else if(propertyValue is IMultiAccessorBase)
			{
				GenerateXHTMLForVirtualStrings((ICmObject)field, (IMultiAccessorBase)propertyValue, config, writer, cache);
			}
			else if(propertyValue is String)
			{
				var propValueString = (String)propertyValue;
				if(!String.IsNullOrEmpty(propValueString))
				{
					// write out Strings something like: <span class="foo">Bar</span>
					writer.WriteStartElement("span");
					WriteClassNameAttribute(writer, config);
					writer.WriteString(propValueString);
					writer.WriteEndElement();
				}
			}
			else
			{
				Debug.WriteLine("What do I do with {0}?", (object)propertyValue.GetType().Name);
			}
		}

		private static void WriteElementContents(object propertyValue, ConfigurableDictionaryNode config,
															  XmlWriter writer)
		{
			writer.WriteStartElement(GetElementNameForProperty(config));
			WriteClassNameAttribute(writer, config);
			writer.WriteString(propertyValue.ToString());
			writer.WriteEndElement();
		}

		/// <summary>
		/// This method will generate an XHTML span with a string for each selected writing system in the
		/// DictionaryWritingSystemOptions of the configuration that also has data in the given IMultiStringAccessor
		/// </summary>
		/// <param name="multiStringAccessor"></param>
		/// <param name="config"></param>
		/// <param name="writer"></param>
		/// <param name="cache"></param>
		private static void GenerateXHTMLForStrings(IMultiStringAccessor multiStringAccessor, ConfigurableDictionaryNode config, XmlWriter writer, FdoCache cache)
		{
			var wsOptions = config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			if(wsOptions == null)
			{
				throw new ArgumentException(@"Configuration nodes for MultiString fields should have WritingSystemOptions", "config");
			}
			foreach(var option in wsOptions.Options)
			{
				if(!option.IsEnabled)
				{
					continue;
				}
				var wsId = WritingSystemServices.GetMagicWsIdFromName(option.Id);
				// The string for the specific wsId in the option, or the best string option in the accessor if the wsId is magic
				ITsString bestString;
				if(wsId == 0)
				{
					// This is not a magic writing system, so grab the user requested string
					wsId = cache.WritingSystemFactory.GetWsFromStr(option.Id);
					bestString = multiStringAccessor.get_String(wsId);
				}
				else
				{
					// Writing system is magic i.e. 'best vernacular' or 'first pronunciation'
					// use the method in the multi-string to get the right string and set wsId to the used one
					bestString = multiStringAccessor.GetAlternativeOrBestTss(wsId, out wsId);
				}
				GenerateWsPrefixAndString(config, writer, cache, wsOptions, wsId, bestString);
			}
		}

		/// <summary>
		/// This method will generate an XHTML span with a string for each selected writing system in the
		/// DictionaryWritingSystemOptions of the configuration that also has data in the given IMultiAccessorBase
		/// </summary>
		/// <param name="owningObject">The object used to access the virtual property</param>
		/// <param name="multiStringAccessor">Virtual Property Accessor</param>
		/// <param name="config"></param>
		/// <param name="writer"></param>
		/// <param name="cache"></param>
		private static void GenerateXHTMLForVirtualStrings(ICmObject owningObject, IMultiAccessorBase multiStringAccessor,
																			ConfigurableDictionaryNode config, XmlWriter writer, FdoCache cache)
		{
			var wsOptions = config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			if(wsOptions == null)
			{
				throw new ArgumentException(@"Configuration nodes for MultiString fields should have WritingSystemOptions", "config");
			}
			foreach(var option in wsOptions.Options)
			{
				if(!option.IsEnabled)
				{
					continue;
				}
				var wsId = WritingSystemServices.GetMagicWsIdFromName(option.Id);
				// The string for the specific wsId in the option, or the best string option in the accessor if the wsId is magic
				if(wsId == 0)
				{
					// This is not a magic writing system, so grab the user requested string
					wsId = cache.WritingSystemFactory.GetWsFromStr(option.Id);
				}
				else
				{
					var defaultWs = owningObject.Cache.WritingSystemFactory.get_EngineOrNull(owningObject.Cache.DefaultUserWs);
					wsId = WritingSystemServices.InterpretWsLabel(owningObject.Cache, option.Id, (IWritingSystem)defaultWs,
																					owningObject.Hvo, multiStringAccessor.Flid, (IWritingSystem)defaultWs);
				}
				var requestedString = multiStringAccessor.get_String(wsId);
				GenerateWsPrefixAndString(config, writer, cache, wsOptions, wsId, requestedString);
			}
		}

		private static void GenerateWsPrefixAndString(ConfigurableDictionaryNode config,
																		 XmlWriter writer, FdoCache cache, DictionaryNodeWritingSystemOptions wsOptions,
																		 int wsId, ITsString requestedString)
		{
			if(String.IsNullOrEmpty(requestedString.Text))
			{
				return;
			}
			if(wsOptions.DisplayWritingSystemAbbreviations)
			{
				writer.WriteStartElement("span");
				writer.WriteStartAttribute("class", "writingsystemprefix");
				var prefix = ((IWritingSystem)cache.WritingSystemFactory.get_EngineOrNull(wsId)).Abbreviation;
				writer.WriteString(prefix);
				writer.WriteEndElement();
			}
			writer.WriteStartElement("span");
			WriteClassNameAttribute(writer, config);
			var wsName = cache.WritingSystemFactory.get_EngineOrNull(wsId).Id;
			GenerateXHTMLForString(requestedString, config, writer, cache, wsName);
			writer.WriteEndElement();
		}

		private static void GenerateXHTMLForString(ITsString fieldValue,
																 ConfigurableDictionaryNode config,
																 XmlWriter writer, FdoCache cache, string writingSystem = null)
		{
			//use the passed in writing system unless null
			//otherwise use the first option from the DictionaryNodeWritingSystemOptions or english if the options are null
			writingSystem = writingSystem ?? GetLanguageFromFirstOption(config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions, cache);
			writer.WriteAttributeString("lang", writingSystem);
			writer.WriteString(fieldValue.Text);
		}

		/// <summary>
		/// This method is intended to produce the xhtml element that we want for given configuration objects.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		private static string GetElementNameForProperty(ConfigurableDictionaryNode config)
		{
			//TODO: Improve this logic to deal with subentries if necessary
			if(config.FieldDescription.Equals("LexEntry") || config.DictionaryNodeOptions is DictionaryNodePictureOptions)
			{
				return "div";
			}
			return "span";
		}

		/// <summary>
		/// This method returns the lang attribute value from the first selected writing system in the given options.
		/// </summary>
		/// <param name="wsOptions"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		private static string GetLanguageFromFirstOption(DictionaryNodeWritingSystemOptions wsOptions, FdoCache cache)
		{
			const string defaultLang = "en";
			if(wsOptions == null)
				return defaultLang;
			foreach(var option in wsOptions.Options)
			{
				if(option.IsEnabled)
				{
					var wsId = WritingSystemServices.GetMagicWsIdFromName(option.Id);
					// if the writing system isn't a magic name just use it
					if( wsId == 0)
					{
						return option.Id;
					}
					// otherwise get a list of the writing systems for the magic name, and use the first one
					return WritingSystemServices.GetWritingSystemList(cache, wsId, true).First().Id;
				}
			}
			// paranoid fallback to first option of the list in case there are no enabled options
			return wsOptions.Options[0].Id;
		}

		public static DictionaryPublicationDecorator GetPublicationDecoratorAndEntries(Mediator mediator, out int[] entriesToSave)
		{
			var cache = mediator.PropertyTable.GetValue("cache") as FdoCache;
			if(cache == null)
			{
				throw new ArgumentException(@"Mediator had no cache", "mediator");
			}
			var clerk = mediator.PropertyTable.GetValue("ActiveClerk", null) as RecordClerk;
			if(clerk == null)
			{
				throw new ArgumentException(@"Mediator had no clerk", "mediator");
			}

			ICmPossibility currentPublication;
			var currentPublicationString = mediator.PropertyTable.GetStringProperty("SelectedPublication", xWorksStrings.AllEntriesPublication);
			if(currentPublicationString == xWorksStrings.AllEntriesPublication)
			{
				currentPublication = null;
			}
			else
			{
				currentPublication =
					(from item in cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS
					 where item.Name.UserDefaultWritingSystem.Text == currentPublicationString
					 select item).FirstOrDefault();
			}
			var decorator = new DictionaryPublicationDecorator(cache, clerk.VirtualListPublisher, clerk.VirtualFlid, currentPublication);
			entriesToSave = decorator.VecProp(cache.LangProject.LexDbOA.Hvo, clerk.VirtualFlid);
			return decorator;
		}
	}
}
