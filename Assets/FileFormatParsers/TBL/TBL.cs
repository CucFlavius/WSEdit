using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;


public class TBL
{
    public struct DBHeader
    {
        public string Signature;         // 'DTBL'
        public UInt32 Version;           // Always 0
        public UInt64 TableNameLength;   // Length of table name (UTF-16)
        public UInt64 Unknown;           // Always 0
        public UInt64 RecordSize;        // Size of one record (in bytes)
        public UInt64 FieldCount;        // Number of fields (columns) per record
        public UInt64 DescriptionOffset; // Start offset for the first column description
        public UInt64 RecordCount;       // Total number of records
        public UInt64 FullRecordSize;    // Size of all records (in bytes)
        public UInt64 EntryOffset;       // Start offset for the first record
        public UInt64 MaxId;             // Last id (mostly first field) + 1
        public UInt64 IDLookupOffset;    // Offset to the entry lookup (see below), relative to the end of the header
        public UInt64 Unknown2;          // Always 0

        public bool IsValidTblFile
        {
            get
            {
                return this.Signature == "LBTD";
            }
        }
    }

    public struct Column
    {
        public UInt32 NameLength; // Length of column name (UTF-16)
        public UInt32 Unknown;
        public UInt64 NameOffset; // Position of the column name
        public UInt16 DataType;   // Column data type
        public UInt16 Unknown2;
        public UInt32 Unknown3;
        public string Name;
    }

    public class DataTable
    {
        public List<List<object>> dataRows = new List<List<object>>();
        public Dictionary<string, Type> Columns = new Dictionary<string, Type>();
        public List<Type> ColumnTypes = new List<Type>();
    }

    public static DataTable Read(string path)
    {
        byte[] inputData = DataManager.GetFileBytes(path);

        List<Column> list = new List<Column>();
        DataTable table = new DataTable();

        try
        {
            using (BinaryReader binaryReader = new BinaryReader(new MemoryStream(inputData)))
            {
                DBHeader dBHeader = new DBHeader();
                dBHeader.Signature = binaryReader.ReadString(4, false);
                dBHeader.Version = binaryReader.ReadUInt32();
                dBHeader.TableNameLength = binaryReader.ReadUInt64();
                dBHeader.Unknown = binaryReader.ReadUInt64();
                dBHeader.RecordSize = binaryReader.ReadUInt64();
                dBHeader.FieldCount = binaryReader.ReadUInt64();
                dBHeader.DescriptionOffset = binaryReader.ReadUInt64();
                dBHeader.RecordCount = binaryReader.ReadUInt64();
                dBHeader.FullRecordSize = binaryReader.ReadUInt64();
                dBHeader.EntryOffset = binaryReader.ReadUInt64();
                dBHeader.MaxId = binaryReader.ReadUInt64();
                dBHeader.IDLookupOffset = binaryReader.ReadUInt64();
                dBHeader.Unknown2 = binaryReader.ReadUInt64();
                if (dBHeader.IsValidTblFile)
                {
                    string str = binaryReader.ReadString((int)dBHeader.TableNameLength, true);

                    //table.BeginLoadData();
                    for (uint num = 0u; num < dBHeader.FieldCount; num++)
                    {
                        binaryReader.BaseStream.Position = (long)(dBHeader.DescriptionOffset + 96 + 24 * num);
                        Column column = new Column();
                        column.NameLength = binaryReader.ReadUInt32();
                        column.Unknown = binaryReader.ReadUInt32();
                        column.NameOffset = binaryReader.ReadUInt64();
                        column.DataType = binaryReader.ReadUInt16();
                        column.Unknown2 = binaryReader.ReadUInt16();
                        column.Unknown3 = binaryReader.ReadUInt32();
                        Column column2 = column;
                        long num2 = (long)(96 + dBHeader.FieldCount * 24 + dBHeader.DescriptionOffset + column2.NameOffset);
                        binaryReader.BaseStream.Position = ((dBHeader.FieldCount % 2uL == 0) ? num2 : (num2 + 8));
                        column2.Name = binaryReader.ReadString((int)(column2.NameLength - 1), true);
                        table.Columns.Add(column2.Name, null);
                        list.Add(column2);
                        switch (column2.DataType)
                        {
                            case 3:
                                table.ColumnTypes.Add(typeof(uint));
                                //table.Columns[column2.Name] = typeof(uint);
                                break;
                            case 4:
                                table.ColumnTypes.Add(typeof(float));
                                //table.Columns[column2.Name] = typeof(float);
                                break;
                            case 11:
                                table.ColumnTypes.Add(typeof(string));
                                //table.Columns[column2.Name] = typeof(string);
                                break;
                            case 20:
                                table.ColumnTypes.Add(typeof(ulong));
                                //table.Columns[column2.Name] = typeof(ulong);
                                break;
                            case 130:
                                table.ColumnTypes.Add(typeof(string));
                                //table.Columns[column2.Name] = typeof(string);
                                break;
                            default:
                                Console.Log("Not supported data type '" + column2.DataType + "'", Console.LogType.Error);
                                break;
                        }
                    }
                    ulong num3 = dBHeader.EntryOffset + 96;

                    for (uint num4 = 0u; num4 < dBHeader.RecordCount; num4++)
                    {
                        List<object> dataRow = new List<object>();
                        int num5 = 0;
                        bool flag = false;
                        binaryReader.BaseStream.Position = (long)(num3 + dBHeader.RecordSize * num4);
                        for (int i = 0; i < (int)dBHeader.FieldCount; i++)
                        {
                            Column column3 = list[i];
                            if (flag && column3.DataType != 130 && num5 == 130)
                            {
                                binaryReader.BaseStream.Position += 4L;
                            }
                            switch (column3.DataType)
                            {
                                case 3:
                                    dataRow.Add(binaryReader.ReadUInt32());
                                    break;
                                case 4:
                                    dataRow.Add(binaryReader.ReadSingle());
                                    break;
                                case 11:
                                    dataRow.Add(Convert.ToBoolean(binaryReader.ReadUInt32()).ToString());
                                    break;
                                case 20:
                                    dataRow.Add(binaryReader.ReadUInt64());
                                    break;
                                case 130:
                                    {
                                        uint num6 = binaryReader.ReadUInt32();
                                        uint num7 = binaryReader.ReadUInt32();
                                        if (num6 == 0)
                                        {
                                            flag = true;
                                        }
                                        long position = binaryReader.BaseStream.Position;
                                        binaryReader.BaseStream.Position = (long)(((num6 != 0) ? num6 : num7) + num3);
                                        dataRow.Add(binaryReader.ReadWString());
                                        binaryReader.BaseStream.Position = position;
                                        break;
                                    }
                            }
                            num5 = column3.DataType;
                        }
                        table.dataRows.Add(dataRow);
                    }
                    //table.EndLoadData();
                }
            }
        }
        catch (Exception ex)
        {
            Console.Log($"Error while loading : {ex.Message}", Console.LogType.Error);
        }

        return table;
    }

}

public static class DBReaderExtensions
{
    public static string ReadWString(this BinaryReader br)
    {
        byte[] array = new byte[0];
        for (byte b = br.ReadByte(); b != 0; b = br.ReadByte())
        {
            br.BaseStream.Position -= 1L;
            array = array.Combine(br.ReadBytes(2));
        }
        return Encoding.Unicode.GetString(array);
    }

    public static string ReadString(this BinaryReader br, int count, bool unicode = true)
    {
        byte[] bytes = br.ReadBytes(unicode ? (count << 1) : count);
        if (!unicode)
        {
            return Encoding.ASCII.GetString(bytes);
        }
        return Encoding.Unicode.GetString(bytes);
    }

    public static byte[] Combine(this byte[] data, byte[] data2)
    {
        byte[] array = new byte[data.Length + data2.Length];
        Buffer.BlockCopy(data, 0, array, 0, data.Length);
        Buffer.BlockCopy(data2, 0, array, data.Length, data2.Length);
        return array;
    }
}
