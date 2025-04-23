export function isActive(
  url: string,
  pathname: string,
  nested = true,
): boolean {
  if (url.endsWith('/')) url = url.slice(0, -1);
  if (pathname.endsWith('/')) pathname = pathname.slice(0, -1);

  return url === pathname || (nested && pathname.startsWith(`${url}/`));
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


