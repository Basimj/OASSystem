using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace OAS.Client.Accounting.Pages;

// Standalone Open XML export; text values are inline strings (never formulas).
internal static class CustomerDebtsWorkbook
{
    private const string Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    internal static byte[] Create(string subtitle, string[] headers, IEnumerable<object?[]> rows, bool suppliers = false, string? sheetName = null, IReadOnlyList<string>? pairedHeaderGroups = null)
    {
        using var memory = new MemoryStream();
        using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, true))
        {
            Add(zip, "[Content_Types].xml", """
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>
                """);
            Add(zip, "_rels/.rels", """
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
                """);
            Add(zip, "xl/workbook.xml", $$"""
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="{{(sheetName ?? (suppliers ? "مستحقات الموردين" : "مديونية العملاء"))}}" sheetId="1" r:id="rId1"/></sheets></workbook>
                """);
            Add(zip, "xl/_rels/workbook.xml.rels", """
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>
                """);
            using var stream = zip.CreateEntry("xl/worksheets/sheet1.xml").Open();
            using var xml = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = false });
            xml.WriteStartDocument(); xml.WriteStartElement("worksheet", Ns);
            xml.WriteStartElement("sheetViews", Ns); xml.WriteStartElement("sheetView", Ns); xml.WriteAttributeString("rightToLeft", "1"); xml.WriteAttributeString("workbookViewId", "0"); xml.WriteEndElement(); xml.WriteEndElement();
            xml.WriteStartElement("sheetData", Ns);
            var index=1;
            WriteRow(xml,index++, [subtitle]);
            if (pairedHeaderGroups is null)
                WriteRow(xml,index++,headers.Cast<object?>());
            else
            {
                if (headers.Length != 2 + pairedHeaderGroups.Count * 2)
                    throw new ArgumentException("Invalid paired header dimensions.", nameof(pairedHeaderGroups));
                WriteRow(xml,index++, new object?[] { headers[0], headers[1] }.Concat(pairedHeaderGroups.SelectMany(g => new object?[] { g, null })));
                WriteRow(xml,index++, new object?[] { null, null }.Concat(pairedHeaderGroups.SelectMany(_ => new object?[] { "مدين", "دائن" })));
            }
            foreach(var row in rows) WriteRow(xml,index++,row);
            xml.WriteEndElement();
            if (pairedHeaderGroups is not null)
            {
                xml.WriteStartElement("mergeCells", Ns);
                xml.WriteAttributeString("count", (pairedHeaderGroups.Count + 2).ToString(CultureInfo.InvariantCulture));
                void Merge(string range) { xml.WriteStartElement("mergeCell", Ns); xml.WriteAttributeString("ref", range); xml.WriteEndElement(); }
                Merge("A2:A3"); Merge("B2:B3");
                for (var i = 0; i < pairedHeaderGroups.Count; i++) Merge($"{ColumnName(3+i*2)}2:{ColumnName(4+i*2)}2");
                xml.WriteEndElement();
            }
            xml.WriteEndElement(); xml.WriteEndDocument();
        }
        return memory.ToArray();
    }
    private static void Add(ZipArchive zip,string path,string contents)
    {
        using var writer=new StreamWriter(zip.CreateEntry(path).Open(),new UTF8Encoding(false)); writer.Write(contents);
    }
    private static string ColumnName(int column)
    {
        var name = string.Empty;
        while (column > 0) { column--; name = (char)('A' + column % 26) + name; column /= 26; }
        return name;
    }
    private static void WriteRow(XmlWriter xml,int row,IEnumerable<object?> values)
    {
        xml.WriteStartElement("row",Ns);xml.WriteAttributeString("r",row.ToString(CultureInfo.InvariantCulture));
        var col=0;
        foreach(var value in values)
        {
            col++;xml.WriteStartElement("c",Ns);xml.WriteAttributeString("r",$"{ColumnName(col)}{row}");
            if(value is decimal or int or long)
                xml.WriteElementString("v",Ns,Convert.ToString(value,CultureInfo.InvariantCulture));
            else
            {
                xml.WriteAttributeString("t","inlineStr");xml.WriteStartElement("is",Ns);
                // Keep valid XML characters; never interpret strings as spreadsheet formulas.
                xml.WriteElementString("t",Ns,new string((value?.ToString()??"").Where(XmlConvert.IsXmlChar).ToArray()));
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
        }
        xml.WriteEndElement();
    }
}
