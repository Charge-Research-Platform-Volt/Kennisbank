import Link from "next/link";

export default function Home() {
  return (
    <div>
      <div>Hello World!</div>
      <Link href="/guide" className="underline">
        Guide
      </Link>
      <br />
      <Link href="/docs" className="underline">
        Docs
      </Link>
    </div>
  );
}
