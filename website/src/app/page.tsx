import Navbar from "@/components/navbar";
import Link from "next/link";
import { ArrowRight, BookOpen, Code, Users, Lightbulb } from "lucide-react";
import Logo from "@/components/logo";

export default function Home() {
    return (
        <main className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-indigo-50 dark:from-gray-900 dark:via-gray-800 dark:to-blue-900">
            <div className="container mx-auto px-4">
                <Navbar />
                
                {/* Hero Section */}
                <section className="py-20 text-center">
                    <div className="max-w-4xl mx-auto">
                        <h1 className="text-5xl md:text-6xl font-bold text-gray-900 dark:text-white mb-6">
                            Welcome to <span className="text-blue-600 dark:text-blue-400">Ohmega Help</span>
                        </h1>
                        <p className="text-xl md:text-2xl text-gray-600 dark:text-gray-300 mb-8 leading-relaxed">
                            Your comprehensive guide to getting started, understanding the platform, and improving your workflow!
                        </p>
                        <div className="flex flex-col sm:flex-row gap-4 justify-center">
                            <Link 
                                href="/guide"
                                className="inline-flex items-center px-8 py-4 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700 transition-colors shadow-lg hover:shadow-xl"
                            >
                                Get Started
                                <ArrowRight className="ml-2 h-5 w-5" />
                            </Link>
                            <Link 
                                href="/docs"
                                className="inline-flex items-center px-8 py-4 bg-white dark:bg-gray-800 text-gray-900 dark:text-white border border-gray-300 dark:border-gray-600 rounded-lg font-semibold hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors shadow-lg hover:shadow-xl"
                            >
                                View Documentation
                                <BookOpen className="ml-2 h-5 w-5" />
                            </Link>
                        </div>
                    </div>
                </section>

                {/* Features Section */}
                <section className="py-16">
                    <div className="max-w-6xl mx-auto">
                        <h2 className="text-3xl md:text-4xl font-bold text-center text-gray-900 dark:text-white mb-12">
                            Everything you need to succeed
                        </h2>
                        <div className="grid md:grid-cols-3 gap-8">
                            {/* Quick Start Guide */}
                            <div className="bg-white dark:bg-gray-800 p-8 rounded-xl shadow-lg hover:shadow-xl transition-shadow border border-gray-200 dark:border-gray-700">
                                <div className="bg-green-100 dark:bg-green-900 w-12 h-12 rounded-lg flex items-center justify-center mb-6">
                                    <Lightbulb className="h-6 w-6 text-green-600 dark:text-green-400" />
                                </div>
                                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">Quick Start Guide</h3>
                                <p className="text-gray-600 dark:text-gray-300 mb-6">
                                    Get up and running in minutes with our step-by-step guide that walks you through the basics.
                                </p>
                                <Link 
                                    href="/guide"
                                    className="inline-flex items-center text-green-600 dark:text-green-400 font-medium hover:text-green-700 dark:hover:text-green-300"
                                >
                                    Start Learning
                                    <ArrowRight className="ml-1 h-4 w-4" />
                                </Link>
                            </div>

                            {/* Documentation */}
                            <div className="bg-white dark:bg-gray-800 p-8 rounded-xl shadow-lg hover:shadow-xl transition-shadow border border-gray-200 dark:border-gray-700">
                                <div className="bg-blue-100 dark:bg-blue-900 w-12 h-12 rounded-lg flex items-center justify-center mb-6">
                                    <Code className="h-6 w-6 text-blue-600 dark:text-blue-400" />
                                </div>
                                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">Complete Documentation</h3>
                                <p className="text-gray-600 dark:text-gray-300 mb-6">
                                    Comprehensive API reference, detailed guides, and technical documentation for developers.
                                </p>
                                <Link 
                                    href="/docs"
                                    className="inline-flex items-center text-blue-600 dark:text-blue-400 font-medium hover:text-blue-700 dark:hover:text-blue-300"
                                >
                                    Browse Docs
                                    <ArrowRight className="ml-1 h-4 w-4" />
                                </Link>
                            </div>

                            {/* Development Team */}
                            <div className="bg-white dark:bg-gray-800 p-8 rounded-xl shadow-lg hover:shadow-xl transition-shadow border border-gray-200 dark:border-gray-700">
                                <div className="bg-purple-100 dark:bg-purple-900 w-12 h-12 rounded-lg flex items-center justify-center mb-6">
                                    <Users className="h-6 w-6 text-purple-600 dark:text-purple-400" />
                                </div>
                                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">Meet the Team</h3>
                                <p className="text-gray-600 dark:text-gray-300 mb-6">
                                    Learn about the developers from Utrecht University who built this platform.
                                </p>
                                <Link 
                                    href="/team"
                                    className="inline-flex items-center text-purple-600 dark:text-purple-400 font-medium hover:text-purple-700 dark:hover:text-purple-300"
                                >
                                    Meet the Team
                                    <ArrowRight className="ml-1 h-4 w-4" />
                                </Link>
                            </div>
                        </div>
                    </div>
                </section>

                {/* CTA Section */}
                <section className="py-16">
                    <div className="max-w-4xl mx-auto text-center bg-gradient-to-r from-blue-600 to-indigo-600 dark:from-blue-700 dark:to-indigo-700 rounded-2xl p-12 text-white shadow-2xl">
                        <h2 className="text-3xl md:text-4xl font-bold mb-6">
                            Ready to get started?
                        </h2>
                        <p className="text-xl mb-8 opacity-90">
                            Jump into our comprehensive guide and start building with Ohmega today.
                        </p>
                        <div className="flex flex-col sm:flex-row gap-4 justify-center">
                            <Link 
                                href="/guide"
                                className="inline-flex items-center px-8 py-4 bg-white text-blue-600 rounded-lg font-semibold hover:bg-gray-100 transition-colors shadow-lg"
                            >
                                Begin Your Journey
                                <ArrowRight className="ml-2 h-5 w-5" />
                            </Link>
                            <Link 
                                href="/changelog"
                                className="inline-flex items-center px-8 py-4 bg-transparent border-2 border-white text-white rounded-lg font-semibold hover:bg-white hover:text-blue-600 transition-colors"
                            >
                                View Changelog
                            </Link>
                        </div>
                    </div>
                </section>

                {/* Footer */}
                <footer className="py-12 text-center text-gray-600 dark:text-gray-400">
                    <div className="max-w-4xl mx-auto">
                        <div className="flex justify-center items-center mb-4">
                            <Logo size="small" />
                        </div>
                        <p className="mb-4">
                            Built with ❤️ by students from Utrecht University (Department of Information and Computing Sciences)
                        </p>
                        <div className="flex justify-center space-x-6 text-sm">
                            <Link href="/guide" className="hover:text-blue-600 dark:hover:text-blue-400 transition-colors">
                                Guide
                            </Link>
                            <Link href="/docs" className="hover:text-blue-600 dark:hover:text-blue-400 transition-colors">
                                Documentation
                            </Link>
                            <Link href="/team" className="hover:text-blue-600 dark:hover:text-blue-400 transition-colors">
                                Team
                            </Link>
                            <Link href="/changelog" className="hover:text-blue-600 dark:hover:text-blue-400 transition-colors">
                                Changelog
                            </Link>
                        </div>
                    </div>
                </footer>
            </div>
        </main>
    );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)