export function renderTemplate({
  template,
  vars,
}: { template: string; vars: object }): string {
  let result = template;

  for (const key of Object.keys(vars)) {
    // @ts-ignore
    const value = vars[key];
    result = result.replaceAll(`{{${key}}}`, value);
  }

  return result;
}
