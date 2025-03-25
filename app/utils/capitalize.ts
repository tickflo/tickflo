export function capitalize(word: string): string {
  if (word.length === 0) {
    return '';
  }

  return word[0].toUpperCase() + word.substring(1);
}
