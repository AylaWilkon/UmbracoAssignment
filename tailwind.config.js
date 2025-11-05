/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Views/**/*.cshtml",
    "./Views/**/*.razor",
    "./wwwroot/**/*.html",
    "./wwwroot/**/*.js"
  ],
  theme: {
    extend: {
      colors: {
        'neutral': '#ffffff',
        'white': '#F2EDDC',
        'white-100': '#F7F7F7',
        'dark': '#0E0E0E',
        'primary': '#4F5955',
        'main-light': '#606966',
        'accent': '#F2EDDC',
        'secondary': '#D9C3A9',
        'gray-400': '#4B5264',
        'body-text': '#535656',
        'body-text-100': '#999999',
        'note1': '#9C9EA6',
        'border-gray': '#EAEAEA',
        'border-400': '#C7C8C9',
      },
      fontFamily: {
        'comfortaa': ['Comfortaa', 'sans-serif'],
        'poppins': ['Poppins', 'sans-serif'],
      },
    },
  },
  plugins: [],
}

