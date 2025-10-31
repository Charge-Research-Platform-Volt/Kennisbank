"use client"

import { FileIcon } from "lucide-react"
import OrganisationIcon from "@/icons/organisation-icon";
import PersonIcon from "@/icons/person-icon";
import Link from 'next/link';

export default function NewPage() 
{
    return (
        <div className="container mx-auto px-4 sm:px-6 lg:px-8 max-w-7xl py-5 min-h-screen flex items-center">
            <div className="w-full">
                {/* Header */}
                <div className="text-center mb-12">
                    <h1 className="text-3xl font-semibold mb-2">Create New Entry</h1>
                    <p className="text-gray-600">Choose what to add</p>
                </div>
                
                {/* Cards Grid */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-6 max-w-4xl mx-auto">
                    {/* Resource card */}
                    <Link href="/new/resource">
                        <div className="h-full bg-white border-2 border-gray-200 rounded-lg p-8 hover:border-purple-600 transition-all cursor-pointer" onClick={() => { console.log("test");}}>
                            <div className="w-full flex justify-center mb-3">
                                <FileIcon className="h-10 w-10" />
                            </div>
                            <div className="text-center">
                                <h1 className="text-xl font-semibold mb-2">Resource</h1>
                                <p className="text-gray-600">Upload a file or add an URL with automatic metadata extraction</p>
                            </div>
                        </div>
                    </Link>
                    
                    {/* Person card */}
                    <Link href="/new/person">
                        <div className="h-full bg-white border-2 border-gray-200 rounded-lg p-8 hover:border-purple-600 transition-all cursor-pointer">
                            <div className="w-full flex justify-center mb-3">
                                <PersonIcon className="h-10 w-10" />
                            </div>
                            <div className="text-center">
                                <h1 className="text-xl font-semibold mb-2">Person</h1>
                                <p className="text-gray-600">Add a person to your knowledge base</p>
                            </div>
                        </div>
                    </Link>
                    
                    {/* Organisation card */}
                    <Link href="/new/organisation">
                        <div className="h-full bg-white border-2 border-gray-200 rounded-lg p-8 hover:border-purple-600 transition-all cursor-pointer">
                            <div className="w-full flex justify-center mb-3">
                                <OrganisationIcon className="h-10 w-10" />
                            </div>
                            <div className="text-center">
                                <h1 className="text-xl font-semibold mb-2">Organisation</h1>
                                <p className="text-gray-600">Add an organisation to your knowledge base</p>
                            </div>
                        </div>
                    </Link>
                </div>
            </div>
        </div>
    )
}