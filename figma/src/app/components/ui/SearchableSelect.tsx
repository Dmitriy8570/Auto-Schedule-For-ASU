
import React, { useState, useRef, useEffect } from 'react';
import { ChevronDown, Search, Check, X } from 'lucide-react';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: (string | undefined | null | false)[]) {
  return twMerge(clsx(inputs));
}

interface Option {
  id: string;
  label: string;
  [key: string]: any;
}

interface SearchableSelectProps {
  options: Option[];
  value?: string;
  onChange: (value: string) => void;
  placeholder?: string;
  label?: string;
  disabled?: boolean;
  className?: string;
  onClear?: () => void;
}

export function SearchableSelect({
  options,
  value,
  onChange,
  placeholder = 'Select...',
  label,
  disabled = false,
  className,
  onClear
}: SearchableSelectProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [search, setSearch] = useState('');
  const containerRef = useRef<HTMLDivElement>(null);

  const selectedOption = options.find(opt => opt.id === value);
  
  const filteredOptions = options.filter(opt => 
    opt.label.toLowerCase().includes(search.toLowerCase())
  );

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleSelect = (optionId: string) => {
    onChange(optionId);
    setIsOpen(false);
    setSearch('');
  };

  return (
    <div className={cn("relative w-full", className)} ref={containerRef}>
      {label && <label className="block text-sm font-medium text-slate-700 mb-1">{label}</label>}
      
      <div
        className={cn(
          "flex items-center justify-between w-full px-3 py-2 text-sm bg-white border rounded-md cursor-pointer transition-colors",
          isOpen ? "border-[rgb(26,77,156)] ring-1 ring-[rgb(26,77,156)]" : "border-slate-300 hover:border-slate-400",
          disabled && "opacity-50 cursor-not-allowed bg-slate-100"
        )}
        onClick={() => !disabled && setIsOpen(!isOpen)}
      >
        <span className={cn("truncate", !selectedOption && "text-slate-500")}>
          {selectedOption ? selectedOption.label : placeholder}
        </span>
        <div className="flex items-center gap-1">
          {selectedOption && onClear && !disabled && (
             <div 
               onClick={(e) => {
                 e.stopPropagation();
                 onClear();
               }}
               className="p-1 hover:bg-slate-100 rounded-full text-slate-400 hover:text-red-500 transition-colors"
             >
               <X className="w-3 h-3" />
             </div>
          )}
          <ChevronDown className={cn("w-4 h-4 text-slate-400 transition-transform", isOpen && "rotate-180")} />
        </div>
      </div>

      {isOpen && (
        <div className="absolute z-50 w-full mt-1 bg-white border border-slate-200 rounded-md shadow-lg max-h-60 overflow-hidden flex flex-col animate-in fade-in zoom-in-95 duration-100">
          <div className="p-2 border-b border-slate-100">
            <div className="relative">
              <Search className="absolute left-2 top-2.5 w-4 h-4 text-slate-400" />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Поиск..."
                className="w-full pl-8 pr-3 py-2 text-sm bg-slate-50 border-none rounded-md focus:ring-1 focus:ring-[rgb(26,77,156)] outline-none"
                autoFocus
                onClick={(e) => e.stopPropagation()}
              />
            </div>
          </div>
          
          <div className="overflow-y-auto flex-1 p-1">
            {filteredOptions.length === 0 ? (
              <div className="px-3 py-8 text-center text-sm text-slate-500">
                Ничего не найдено
              </div>
            ) : (
              filteredOptions.map((option) => (
                <div
                  key={option.id}
                  onClick={() => handleSelect(option.id)}
                  className={cn(
                    "flex items-center justify-between px-3 py-2 text-sm rounded-md cursor-pointer transition-colors",
                    value === option.id ? "bg-[rgba(26,77,156,0.1)] text-[rgb(26,77,156)]" : "hover:bg-slate-100 text-slate-700"
                  )}
                >
                  <span>{option.label}</span>
                  {value === option.id && <Check className="w-4 h-4" />}
                </div>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
}
