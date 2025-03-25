export function prettyDate(date: Date): string {
  const month = date.getMonth() + 1;
  return `${date.getFullYear()}-${month < 10 ? '0' : null}${month}-${date.getDate()} ${date.getHours()}:${date.getMinutes()}`;
}
