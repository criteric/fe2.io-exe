FE2.IO DESKTOP
==============

Bu proje FE2.IO sitesini Windows masaustunde ayri bir uygulama penceresinde acmak icin
hazirlanmistir. FE2.IO'nun kendisini kopyalamaz; siteye WebView2 uzerinden baglanir.

Gereksinimler
-------------
- Windows 10/11 x64
- .NET 8 SDK
- Microsoft Edge WebView2 Runtime
- Build sirasinda internet baglantisi (NuGet paketi icin)

DERLEME
-------
1. Bu klasoru cikart.
2. build.bat dosyasina cift tikla.
3. Islem bittiginde dist klasorundeki:
   FE2IO Desktop.exe
   dosyasini calistir.

Notlar
------
- Uygulama FE2.IO'ya baglanir; FE2.IO'daki hesap, harita, muzik ve sunucu
  ozellikleri sitenin kendi sunucu tarafli sistemlerine baglidir.
- Uygulama Roblox'u kendi icinde calistirmaz.
- FE2.IO acilmiyorsa once normal Edge/Chrome'da fe2.io adresinin acildigini
  kontrol et.
- WebView2 Runtime Windows'ta bulunmuyorsa Microsoft Edge WebView2 Runtime
  kurulmalidir.
- Gelistirme amacli DevTools aciktir; F12 ile WebView2 gelistirici araclarini
  kullanabilirsin.
