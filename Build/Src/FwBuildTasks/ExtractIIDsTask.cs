// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2013, SIL International. All Rights Reserved.
// <copyright from='2013' to='2013' company='SIL International'>
//		Copyright (c) 2013, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Extract GUIDs of COM interfaces from Input for building on Linux. This generates a
	/// C++ file that defines the interface GUIDs.
	/// </summary>
	/// <example>
	/// &lt;ExtractIIDs
	///     Input="$(dir-fwoutputcommon)/FwKernelTlb.h"
	///     Output="$(fwrt)/Src/Kernel/FwKernel_GUIDs.cpp"/&gt;
	/// </example>
	/// ----------------------------------------------------------------------------------------
	public class ExtractIIDs: Task
	{
		/// <summary>
		/// Name (and path) of a header file generated by MIDL that contains the COM interface definitions
		/// </summary>
		[Required]
		public string Input { get; set; }

		/// <summary>
		/// Name (and path) of the C++ file that gets generated.
		/// </summary>
		[Required]
		public string Output { get; set; }

		/// <summary>
		/// Lines that will get inserted at the top of the file. Useful to
		/// add additional includes.
		/// </summary>
		public ITaskItem[] LinesToInsertAtTop { get; set; }

		public bool UseUnixNewlines { get; set; }

		public override bool Execute()
		{
			Log.LogMessage(MessageImportance.Normal, "Extracting IIDs from {0}", Input);

			var inputContents = File.ReadAllText(Input);
			var regex = new Regex(@"^\s*(MIDL_INTERFACE|class DECLSPEC_UUID)\(""(........)-(....)-(....)-(..)(..)-(..)(..)(..)(..)(..)(..)""\)\s*\n\s*(?<name>\w+)\s*(:|;)",
				RegexOptions.Multiline | RegexOptions.Singleline);

			using (var outfile = new StreamWriter(Output))
			{
				if (UseUnixNewlines)
					outfile.NewLine = "\n";

				outfile.WriteLine("// Automatically generated from {0} by ExtractIIDs task",
					Path.GetFileName(Input));

				if (LinesToInsertAtTop != null)
				{
					foreach (var line in LinesToInsertAtTop)
						outfile.WriteLine(line.ItemSpec);
				}

				outfile.WriteLine("#include \"{0}\"", Path.GetFileName(Input));
				outfile.WriteLine();

				foreach (Match matchedInterface in regex.Matches(inputContents))
				{
					outfile.WriteLine(
						"DEFINE_UUIDOF({0}, 0x{1}, 0x{2}, 0x{3}, 0x{4}, 0x{5}, 0x{6}, 0x{7}, 0x{8}, 0x{9}, 0x{10}, 0x{11});",
						matchedInterface.Groups["name"], matchedInterface.Groups[2], matchedInterface.Groups[3],
						matchedInterface.Groups[4], matchedInterface.Groups[5], matchedInterface.Groups[6],
						matchedInterface.Groups[7], matchedInterface.Groups[8], matchedInterface.Groups[9],
						matchedInterface.Groups[10], matchedInterface.Groups[11], matchedInterface.Groups[12]);
				}
			}
			return !Log.HasLoggedErrors;
		}
	}
}
