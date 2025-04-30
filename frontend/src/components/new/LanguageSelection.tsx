"use client"

import * as React from "react"
import { Check, ChevronsUpDown } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from "@/components/ui/command"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form"
import { Control, FieldPath, UseFormSetValue } from "react-hook-form"

export function LanguageSelection() 
{
    const [open, setOpen] = React.useState(false);
    const [value, setValue] = React.useState("");
    
    return (
        <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger asChild>
                <Button variant="outline" role="combobox" aria-expanded={open} className="w-full justify-between">
                    { value ? languages.find((language) => language.value === value)?.label : "Select language..." }
                    <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                </Button>
            </PopoverTrigger>
            <PopoverContent className="w-full p-0">
                <Command>
                    <CommandInput placeholder="Search language..." />
                    <CommandList>
                        <CommandEmpty>No language found.</CommandEmpty>
                        <CommandGroup>
                            {
                                languages.map((language) => (
                                    <CommandItem key={language.value} value={language.value} onSelect={(currentValue) => { setValue(currentValue === value ? "" : currentValue); setOpen(false)}}>
                                        {language.label}
                                        <Check className={cn("mr-2 h-4 w-4", value === language.value ? "opacity-100" : "opacity-0")} />
                                    </CommandItem>
                                ))
                            }
                        </CommandGroup>
                    </CommandList>
                </Command>
            </PopoverContent>
        </Popover>
    );
}

interface LanguageSelectionFormFieldProps<T extends object>
{
    control: Control<T>;
    name: FieldPath<T>;
    setValue?: UseFormSetValue<T>;
    label?: string;
    placeholder?: string;
}

export function LanguageSelectionForm<T extends object>({ control, name, setValue, label = "Language", placeholder = "Select language..."}: LanguageSelectionFormFieldProps<T>) 
{
    const [open, setOpen] = React.useState(false);

    return (
        <FormField control={control} name={name} render={({field}) => (
            <FormItem className="flex flex-col">
                <FormLabel>{label}</FormLabel>
                <Popover open={open} onOpenChange={setOpen}>
                    <PopoverTrigger asChild>
                        <FormControl>
                            <Button variant="outline" role="combobox" className={cn("w-full justify-between", !field.value && "text-muted-foreground")} >
                                { field.value ? languages.find((language) => language.value === field.value)?.label : placeholder }
                                <ChevronsUpDown className="opacity-50" />
                            </Button>
                        </FormControl>
                    </PopoverTrigger>
                    <PopoverContent className="w-full p-0">
                        <Command>
                            <CommandInput placeholder="Search language..." />
                            <CommandList>
                                <CommandEmpty>No language found.</CommandEmpty>
                                <CommandGroup>
                                    {
                                        languages.map((language) => (
                                            <CommandItem key={language.value} value={language.label} onSelect={() => { setValue ? setValue(name, language.value as any) : field.onChange(language); setOpen(false) }}>
                                                {language.label}
                                                <Check className={cn("ml-auto", field.value === language.value ? "opacity-100" : "opacity-0")} />
                                            </CommandItem>
                                        ))
                                    }
                                </CommandGroup>
                            </CommandList>
                        </Command>
                    </PopoverContent>
                </Popover>
                <FormMessage />
            </FormItem>
        )} />
    );
}

// All languages and their codes from the ISO 639-1 list
const languages = [
    { value: "ab", label: "Abkhazian" },
    { value: "aa", label: "Afar" },
    { value: "af", label: "Afrikaans" },
    { value: "ak", label: "Akan" },
    { value: "sq", label: "Albanian" },
    { value: "am", label: "Amharic" },
    { value: "ar", label: "Arabic" },
    { value: "an", label: "Aragonese" },
    { value: "hy", label: "Armenian" },
    { value: "as", label: "Assamese" },
    { value: "av", label: "Avaric" },
    { value: "ae", label: "Avestan" },
    { value: "ay", label: "Aymara" },
    { value: "az", label: "Azerbaijani" },
    { value: "bm", label: "Bambara" },
    { value: "ba", label: "Bashkir" },
    { value: "eu", label: "Basque" },
    { value: "be", label: "Belarusian" },
    { value: "bn", label: "Bengali" },
    { value: "bi", label: "Bislama" },
    { value: "bs", label: "Bosnian" },
    { value: "br", label: "Breton" },
    { value: "bg", label: "Bulgarian" },
    { value: "my", label: "Burmese" },
    { value: "ca", label: "Catalan" },
    { value: "ch", label: "Chamorro" },
    { value: "ce", label: "Chechen" },
    { value: "ny", label: "Chichewa" },
    { value: "zh", label: "Chinese" },
    { value: "cv", label: "Chuvash" },
    { value: "kw", label: "Cornish" },
    { value: "co", label: "Corsican" },
    { value: "cr", label: "Cree" },
    { value: "hr", label: "Croatian" },
    { value: "cs", label: "Czech" },
    { value: "da", label: "Danish" },
    { value: "dv", label: "Divehi" },
    { value: "nl", label: "Dutch" },
    { value: "dz", label: "Dzongkha" },
    { value: "en", label: "English" },
    { value: "eo", label: "Esperanto" },
    { value: "et", label: "Estonian" },
    { value: "ee", label: "Ewe" },
    { value: "fo", label: "Faroese" },
    { value: "fj", label: "Fijian" },
    { value: "fi", label: "Finnish" },
    { value: "fr", label: "French" },
    { value: "ff", label: "Fulah" },
    { value: "gl", label: "Galician" },
    { value: "ka", label: "Georgian" },
    { value: "de", label: "German" },
    { value: "el", label: "Greek" },
    { value: "gn", label: "Guarani" },
    { value: "gu", label: "Gujarati" },
    { value: "ht", label: "Haitian" },
    { value: "ha", label: "Hausa" },
    { value: "he", label: "Hebrew" },
    { value: "hz", label: "Herero" },
    { value: "hi", label: "Hindi" },
    { value: "ho", label: "Hiri Motu" },
    { value: "hu", label: "Hungarian" },
    { value: "ia", label: "Interlingua" },
    { value: "id", label: "Indonesian" },
    { value: "ie", label: "Interlingue" },
    { value: "ga", label: "Irish" },
    { value: "ig", label: "Igbo" },
    { value: "ik", label: "Inupiaq" },
    { value: "io", label: "Ido" },
    { value: "is", label: "Icelandic" },
    { value: "it", label: "Italian" },
    { value: "iu", label: "Inuktitut" },
    { value: "ja", label: "Japanese" },
    { value: "jv", label: "Javanese" },
    { value: "kl", label: "Kalaallisut" },
    { value: "kn", label: "Kannada" },
    { value: "kr", label: "Kanuri" },
    { value: "ks", label: "Kashmiri" },
    { value: "kk", label: "Kazakh" },
    { value: "km", label: "Khmer" },
    { value: "ki", label: "Kikuyu" },
    { value: "rw", label: "Kinyarwanda" },
    { value: "ky", label: "Kyrgyz" },
    { value: "kv", label: "Komi" },
    { value: "kg", label: "Kongo" },
    { value: "ko", label: "Korean" },
    { value: "ku", label: "Kurdish" },
    { value: "kj", label: "Kuanyama" },
    { value: "la", label: "Latin" },
    { value: "lb", label: "Luxembourgish" },
    { value: "lg", label: "Ganda" },
    { value: "li", label: "Limburgan" },
    { value: "ln", label: "Lingala" },
    { value: "lo", label: "Lao" },
    { value: "lt", label: "Lithuanian" },
    { value: "lu", label: "Luba-Katanga" },
    { value: "lv", label: "Latvian" },
    { value: "gv", label: "Manx" },
    { value: "mk", label: "Macedonian" },
    { value: "mg", label: "Malagasy" },
    { value: "ms", label: "Malay" },
    { value: "ml", label: "Malayalam" },
    { value: "mt", label: "Maltese" },
    { value: "mi", label: "Maori" },
    { value: "mr", label: "Marathi" },
    { value: "mh", label: "Marshallese" },
    { value: "mn", label: "Mongolian" },
    { value: "na", label: "Nauru" },
    { value: "nv", label: "Navajo" },
    { value: "nd", label: "North Ndebele" },
    { value: "ne", label: "Nepali" },
    { value: "ng", label: "Ndonga" },
    { value: "nb", label: "Norwegian Bokmål" },
    { value: "nn", label: "Norwegian Nynorsk" },
    { value: "no", label: "Norwegian" },
    { value: "ii", label: "Sichuan Yi" },
    { value: "nr", label: "South Ndebele" },
    { value: "oc", label: "Occitan" },
    { value: "oj", label: "Ojibwa" },
    { value: "cu", label: "Church Slavic" },
    { value: "om", label: "Oromo" },
    { value: "or", label: "Oriya" },
    { value: "os", label: "Ossetian" },
    { value: "pa", label: "Punjabi" },
    { value: "pi", label: "Pali" },
    { value: "fa", label: "Persian" },
    { value: "pl", label: "Polish" },
    { value: "ps", label: "Pashto" },
    { value: "pt", label: "Portuguese" },
    { value: "qu", label: "Quechua" },
    { value: "rm", label: "Romansh" },
    { value: "rn", label: "Rundi" },
    { value: "ro", label: "Romanian" },
    { value: "ru", label: "Russian" },
    { value: "sa", label: "Sanskrit" },
    { value: "sc", label: "Sardinian" },
    { value: "sd", label: "Sindhi" },
    { value: "se", label: "Northern Sami" },
    { value: "sm", label: "Samoan" },
    { value: "sg", label: "Sango" },
    { value: "sr", label: "Serbian" },
    { value: "gd", label: "Gaelic" },
    { value: "sn", label: "Shona" },
    { value: "si", label: "Sinhala" },
    { value: "sk", label: "Slovak" },
    { value: "sl", label: "Slovenian" },
    { value: "so", label: "Somali" },
    { value: "st", label: "Southern Sotho" },
    { value: "es", label: "Spanish" },
    { value: "su", label: "Sundanese" },
    { value: "sw", label: "Swahili" },
    { value: "ss", label: "Swati" },
    { value: "sv", label: "Swedish" },
    { value: "ta", label: "Tamil" },
    { value: "te", label: "Telugu" },
    { value: "tg", label: "Tajik" },
    { value: "th", label: "Thai" },
    { value: "ti", label: "Tigrinya" },
    { value: "bo", label: "Tibetan" },
    { value: "tk", label: "Turkmen" },
    { value: "tl", label: "Tagalog" },
    { value: "tn", label: "Tswana" },
    { value: "to", label: "Tonga" },
    { value: "tr", label: "Turkish" },
    { value: "ts", label: "Tsonga" },
    { value: "tt", label: "Tatar" },
    { value: "tw", label: "Twi" },
    { value: "ty", label: "Tahitian" },
    { value: "ug", label: "Uighur" },
    { value: "uk", label: "Ukrainian" },
    { value: "ur", label: "Urdu" },
    { value: "uz", label: "Uzbek" },
    { value: "ve", label: "Venda" },
    { value: "vi", label: "Vietnamese" },
    { value: "vo", label: "Volapük" },
    { value: "wa", label: "Walloon" },
    { value: "cy", label: "Welsh" },
    { value: "wo", label: "Wolof" },
    { value: "fy", label: "Western Frisian" },
    { value: "xh", label: "Xhosa" },
    { value: "yi", label: "Yiddish" },
    { value: "yo", label: "Yoruba" },
    { value: "za", label: "Zhuang" },
    { value: "zu", label: "Zulu" }
  ];