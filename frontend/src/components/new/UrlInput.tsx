interface UrlInputProps {
  value: string;
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  disabled: boolean;
}

export default function UrlInput({ value, onChange, disabled }: UrlInputProps) {
  return (
    <div className={`bg-white border-2 border-gray-300 rounded-xl p-6 transition-opacity ${disabled ? 'opacity-50 pointer-events-none' : ''}`}>
      <label htmlFor="urlInput" className="block text-sm font-medium text-gray-900 mb-2">
        Enter a URL
      </label>
      <input
        type="url"
        id="urlInput"
        value={value}
        onChange={onChange}
        disabled={disabled}
        placeholder="https://example.com/article"
        className="w-full px-4 py-3 border-2 border-gray-300 rounded-lg text-sm
                   focus:outline-none focus:border-purple-600 focus:ring-4 
                   focus:ring-purple-600/10 transition-all
                   disabled:bg-gray-50 disabled:cursor-not-allowed"
      />
    </div>
  );
}