/**
 * 
 * @returns About page with our contact information and the copyright notice.
 */
import Navbar from "@/components/navbar";

export default function Page()
{
    return (
        <main className="container">
            <Navbar />
            <div className="flex flex-col items-center">
                <h1 className="text-2xl font-bold mb-2">About Ωhmega</h1>
                <label className="block mb-4 text-gray-700"> We are a group of students from Utrecht University developing this page for the software project course.</label>
                <h1 className="text-xl mb-2 font-semibold mt-2">Contact information Ωhmega</h1>
                <div className="mb-4 text-gray-700 flex flex-col items-center">
                    <label>Abel Dieterich - Developer / Product Owner</label>
                    <label>Aiden van Dijk - Developer</label>
                    <label>Elia Jabbour - Developer</label>
                    <label>Justin Liem - Developer</label>
                    <label>Rens van Moorsel - Developer</label>
                    <label>Jason van Otterlo - Developer / Chair</label>
                    <label>Jelle van het Schut - Developer</label>
                    <label>Yorick Spekle - Developer / Scrum Master</label>
                    <label>Diogo Landau - Supervisor</label>
                </div>
                <h1 className="text-xl font-semibold mt-2">Copyright Notice</h1>
                <pre className="text-gray-700 whitespace-pre-line mt-2">
                    This program has been developed by students from the bachelor Computer Science at Utrecht<br/>
                    University within the Software Project course.<br/>
                    © Copyright Utrecht University (Department of Information and Computing Sciences)
                </pre>

            </div>
        </main>
    );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)