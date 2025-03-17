export default function AuthTemplate({ children }: { children: React.ReactNode }) {
  return <>{children}</>; // Dit negeert de bovenliggende layout
}
