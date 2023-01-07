using CS_Java_VM.Src.Java.Constants;

using System.IO;
using System;

namespace CS_Java_VM.Src.Java;


public class JavaClass {
  /// <summary>
  /// The constant magic number in the Java class file, this will preceed all other items in the file
  /// </summary>
  private const UInt32 JAVA_CLASS_MAGIC_NUMBER = 0xCAFEBABE;

  /// <summary>
  /// Is the magic number for a Java class file, and will always be 0xCAFEBABE
  /// </summary>
  UInt32 MagicNumber;

  UInt16 MinorVersion, MajorVersion;
  E_AccessFlags AccessFlags;

  UInt16 ThisClass, SuperClass;

  UInt16 ConstantPoolCount;
  IConstantPool[] ConstantPool;

  UInt16 InterfacesCount;
  UInt16[] Interfaces;

  UInt16 FieldsCount;
  FieldsInfo[] Fields;

  UInt16 MethodsCount;
  MethodInfo[] Methods; 

  UInt16 AttributesCount;
  AttributeInfo[] Attributes;

  /// <summary>
  /// 
  /// </summary>
  public JavaClass(string classFilePath) {
    Int32 pointer = 0;
    byte[] bytes = File.ReadAllBytes(classFilePath);

    // Should always be equal 0xCAFEBABE
    MagicNumber = BytesToUInt32(bytes.Skip(pointer).Take(4).ToArray());
    pointer += 4;
    if (MagicNumber != JAVA_CLASS_MAGIC_NUMBER)
      throw new InvalidDataException($"The data in the magic number does not match the know Java class file magic number.{System.Environment.NewLine}The parsor found: {MagicNumber}, but excpected: {JAVA_CLASS_MAGIC_NUMBER}");

    // Get both the minor and major versions of the file, used
    // to figure out if a function is supported later down the line
    MinorVersion = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
    pointer += 2;
    MajorVersion = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
    pointer += 2;

    // Gets the constant pool count, and inits the constant pool
    ConstantPoolCount = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
    pointer += 2;
    ConstantPool = new IConstantPool[ConstantPoolCount-1];
    // Fills the ConstantPool array with the files constants
    for (int i = 0; i < ConstantPoolCount; i++) {
      byte constantPoolTag = bytes.Skip(pointer).Take(1).First();
      pointer++;
      IConstantPool newConstant = ParseConstantPoolTag(constantPoolTag, ref pointer, bytes);
      ConstantPool[i] = newConstant;
    }

    // Gets the AccessFlag from
    AccessFlags = (E_AccessFlags)BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
    pointer += 2;

    // Gets the this class and super class from the Java class file
    ThisClass = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
    pointer += 2;
    SuperClass = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
    pointer += 2;

    // Gets the InterfaceCount and sets the Interfaces array to be InterfaceCount-1
    InterfacesCount = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
    pointer += 2;
    Interfaces = new UInt16[InterfacesCount-1];
    throw new NotImplementedException();

    // Gets the FieldsCount variable and sets the Fields array to be of size FieldsCount-1
    FieldsCount = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
    pointer += 2;
    Fields = new FieldsInfo[FieldsCount-1];
    throw new NotImplementedException();

    // Gets the MethodsCount variable and sets the Methods array to be of size MethodsCount-1
    MethodsCount = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
    pointer += 2;
    Methods = new MethodInfo[MethodsCount-1];
    throw new NotImplementedException();

    // Gets the AttributesCount variable and sets the Attributes array to be of size AttributesCount-1
    AttributesCount = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
    pointer += 2;
    Attributes = new AttributeInfo[AttributesCount-1];
    throw new NotImplementedException();
  }

  /// <summary>
  /// 
  /// </summary>
  private UInt16 BytesToUInt16(byte[] input) {
    UInt16 r = 0;
    for (int i = 0; i< 2; i++) {
      r = (UInt16)(r << 8);
      r += input[i];
    }
    return r;
  }

  /// <summary>
  /// 
  /// </summary>
  private UInt32 BytesToUInt32(byte[] input) {
    UInt32 r = 0;
    for (int i = 0; i< 4; i++) {
      r = r << 8;
      r += input[i];
    }
    return r;
  }

  /// <summary>
  /// Handels the parsing for the tags, this will not icroment the pointer globaly
  /// </summary>
  /// <param name="tag"> The tag that is being parsed this round </param>
  /// <param name="pointer"> The current position of the array pointer </param>
  /// <param name="bytes"> The array of bites that is being parsed this round </param>
  private IConstantPool ParseConstantPoolTag(byte tag, ref int pointer, byte[] bytes) {
    IConstantPool? result = null;

    switch ((E_ConstantPoolTag)tag) {
      case E_ConstantPoolTag.CONSTANT_Utf8:
        UInt16 utf8Length = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
        pointer += 2;
        result = new ConstantPoolUtf8Info(tag, utf8Length);
        ConstantPoolUtf8Info holder = (ConstantPoolUtf8Info)result;
        GetUtf8Bytes(ref holder, ref pointer, bytes);
        result = holder;
      break;

      case E_ConstantPoolTag.CONSTANT_Integer:
      case E_ConstantPoolTag.CONSTANT_Float:
        UInt32 numberBytes = BytesToUInt32(bytes.Skip(pointer).Take(4).ToArray());
        pointer += 4;
        result = new ConstantPoolNumberInfo(tag, numberBytes);
      break;

      case E_ConstantPoolTag.CONSTANT_Long:
      case E_ConstantPoolTag.CONSTANT_Double:
        UInt32 highBytes = BytesToUInt32(bytes.Skip(pointer).Take(4).ToArray());
        pointer += 4;
        UInt32 lowBytes = BytesToUInt32(bytes.Skip(pointer).Take(4).ToArray());
        pointer += 4;
        result = new ConstantPoolLongDoubleInfo(tag, highBytes, lowBytes);
      break;

      case E_ConstantPoolTag.CONSTANT_Class:
        UInt16 classNameIndex = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
        pointer += 2;
        result = new ConstantPoolClass(tag, classNameIndex);
      break;

      case E_ConstantPoolTag.CONSTANT_String:
        UInt16 stringIndex = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
        pointer += 2;
        result = new ConstantPoolString(tag, stringIndex);
      break;

      case E_ConstantPoolTag.CONSTNAT_Fieldref:
      case E_ConstantPoolTag.CONSTANT_Methodref:
      case E_ConstantPoolTag.CONSTANT_InterfaceMethodref:
        UInt16 classIndex = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
        pointer += 2;
        UInt16 refNameAndTypeIndex = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
        pointer += 2;
        result = new ConstantPoolRef(tag, classIndex, refNameAndTypeIndex);
      break;

      case E_ConstantPoolTag.CONSTANT_NameAndType:
        UInt16 nameAndTypeNameIndex = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
        pointer += 2;
        UInt16 nameAndTypeDescriptorIndex = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
        pointer += 2;
        result = new ConstantPoolNameAndTypeInfo(tag, nameAndTypeNameIndex, nameAndTypeDescriptorIndex);
      break;

      case E_ConstantPoolTag.CONSTANT_MethodHandle:
        E_ReferenceKind referenceKind = (E_ReferenceKind)bytes.Skip(pointer).First();
        pointer++;
        UInt16 referenceIndex = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
        pointer += 2;
        result = new ConstantPoolMethodHandleInfo((E_ConstantPoolTag)tag, referenceKind, referenceIndex);
      break;

      case E_ConstantPoolTag.CONSTANT_MethodType:
        UInt16 methodTypeDescriptorIndex = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
        pointer += 2;
        result = new ConstantPoolMethodTypeInfo(tag, methodTypeDescriptorIndex);
      break;

      case E_ConstantPoolTag.CONSTANT_Dynamic:
      case E_ConstantPoolTag.CONSTANT_InvokeDynamic:
        UInt16 bootstrapMethodAttrIndex = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
        pointer += 2;
        UInt16 dynamicNameAndTypeIndex = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
        pointer += 2;
        result = new ConstantPoolDynamicInfo(tag, bootstrapMethodAttrIndex, dynamicNameAndTypeIndex);
      break;

      case E_ConstantPoolTag.CONSTANT_Module:
      case E_ConstantPoolTag.CONSTANT_Package:
        UInt16 packageModuleNameIndex = BytesToUInt16(bytes.Skip(pointer).Take(2).ToArray());
        pointer += 2;
        result = new ConstantPoolPackageModuleInfo(tag, packageModuleNameIndex);
      break;
      default:
        throw new Exception("Unrechable Code");
    }

    if (result == null)
      throw new Exception("Unrechable Code");

    return result;
  }

  private void GetUtf8Bytes(ref ConstantPoolUtf8Info result, ref int pointer, byte[] bytes) {
    throw new NotImplementedException();
  }
}
