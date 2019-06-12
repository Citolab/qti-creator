# Citolab qti-creator for .NET
 
This library helps to create a qti-package with simple item-types and scoring. 
It uses Razor templates to create a qti-package. 
It's build to do conversions and never created to be production tool. 

## Usage

With ASP.NET Core:

```C#
var services = new ServiceCollection();
services.AddQtiCreator();
```

Or just create an instance in .NET (Core)

```C#
var qtiCreator = new QtiPackageCreator();
```

It's important to make sure PreserveCompilationContext is set to `true`

```XML
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>

    <PreserveCompilationContext>true</PreserveCompilationContext>
    <TargetFramework>netcoreapp2.2</TargetFramework>
  </PropertyGroup>

</Project>
```

## API

Currently it supports multiple choice and textEntry with simple scoring only.

There are two methods:

### CreatePackageWithPlainTextItems 

Creates a package with items with plain text or QTI compatible html. A simple `<div>alternative A</div>`
will be QTI compatible. But if there is a inline styles: `div style='color: red;'`  or images then you should use the richtext method.

### CreatePackageWithRichTextItems 

Creates a package with items with rich text. The content of item.body and alternatives will be converted
to qti compatible html. You should provide a handler to process images. The input will the src of the image and the return value should be of IRetrievedFile. This because images can be in a database, file location or base64.


