# ApkRenamer

重命名Apk/xapk文件


> Usage: ApkRenamer apk-path pattern
> pattern: |label| or |application-name| or |name|, |version-name|, |version-code|
> , |package-name|
> Default pattern is |label|(|version-name|).apk


使用了[Iteedee.ApkReader](https://github.com/hylander0/Iteedee.ApkReader)来读取Apk信息, 实际使用中有些Apk无法正确读取.
以后改成直接使用aapt
