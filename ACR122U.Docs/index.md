---
_layout: landing
---

## ACR122U

Simple library to read NFC card from ACR122U card reader.


### Example

```
using FrApp42.ACR122U

...

Reader _reader = new Reader();

_reader.Connected += OnReaderConnected;
_reader.Disconnected += OnReaderDisconnected;
_reader.Inserted += OnCardInserted;
_reader.Removed += OnCardRemoved;

...

private void OnReaderConnected(string value)
{
    Console.WriteLine($"New reader connected : {value}");
}

private void OnReaderDisconnected(string value)
{
    Console.WriteLine($"Reader disconnected : {value}");
}

private async void OnCardInserted(string uid)
{
    Console.WriteLine($"New card detected : {uid}");
}

private void OnCardRemoved()
{
    Console.WriteLine("Card removed");
}

```