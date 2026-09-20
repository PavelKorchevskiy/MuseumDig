using Godot;

public static class LocalizationManager
{
    private static string _currentLanguage = "en";

    public static void SetLanguage(string language)
    {
        _currentLanguage = language;
        TranslationServer.SetLocale(language);
        GD.Print($"[Localization] Язык изменен на: {language}");
    }

    public static string GetLanguage()
    {
        return _currentLanguage;
    }

    /// <summary>
    /// Получает переведенную строку по ключу
    /// </summary>
        public static string Tr(string key)
    {
        string translated = TranslationServer.Translate(key);
        
        // Если перевод не найден, TranslationServer возвращает сам ключ
        if (translated == key)
        {
            GD.PrintErr($"[Localization] ⚠️ Перевод не найден для ключа: {key}");
        }
        
        return translated;
    }

    /// <summary>
    /// Получает переведенную строку с форматированием
    /// Пример: TrFormat("ui.inventory.total_value", 1500) → "Общая стоимость: 1500 монет"
    /// </summary>
    public static string TrFormat(string key, params object[] args)
    {
        string translated = TranslationServer.Translate(key);
        return string.Format(translated, args);
    }

}