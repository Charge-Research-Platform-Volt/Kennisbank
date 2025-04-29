/**
 *
 * @returns About page with our contact information and the copyright notice.
 */
import Navbar from "@/components/navbar";

export default function Page() {
    return (
        <main className="container">
            <Navbar />
            <div className="flex flex-col items-center">
                <h1 className="mb-2 text-2xl font-bold">About Ωhmega</h1>
                <div className="mb-4 block text-gray-700">
                    {" "}
                    We are a group of students from Utrecht University
                    developing this page for the software project course.
                </div>
                <h1 className="mt-2 mb-2 text-xl font-semibold">
                    Contact information Ωhmega
                </h1>
                <div className="mb-4 flex flex-col items-center text-gray-700">
                    <div>Abel Dieterich - Product Owner / Developer</div>
                    <div>Aiden van Dijk - Developer</div>
                    <div>Elia Jabbour - Product Owner / Developer</div>
                    <div>Justin Liem - Developer</div>
                    <div>Rens van Moorsel - Developer</div>
                    <div>Jason van Otterlo - Developer / Chair</div>
                    <div>Jelle van het Schut - Developer</div>
                    <div>Yorick Spekle - Developer / Scrum Master</div>
                    <div>Diogo Landau - Supervisor</div>
                </div>
                <h1 className="mt-2 text-xl font-semibold">Copyright Notice</h1>
                <pre className="mt-2 whitespace-pre-line text-gray-700">
                    This program has been developed by students from the
                    bachelor Computer Science at Utrecht
                    <br />
                    University within the Software Project course.
                    <br />© Copyright Utrecht University (Department of
                    Information and Computing Sciences)
                </pre>
            </div>
        </main>
    );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
